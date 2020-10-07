SonarLintTracker
----------------

The tagger/error list integration code is based on the in the VS Extensibility "Error List" sample
at https://github.com/Microsoft/VSSDK-Extensibility-Samples/tree/master/ErrorList

Important terms in Visual Studio development:

- Tags: text markers in the editor, such as underlinings of errors
- Error List: is the general purpose view for all kinds of errors

In a nutshell:
- there are two separate areas of functionality:
  1) editor integration i.e. showing squigglies and tooltips in the editor
  2) error list integration i.e. showing errors in the error list

The `TaggerProvider` is the glue that holds everthing together:
- it is a singleton that will be created by VS when required
- it has an ISonarErrorListDataSource that it uses to update the Error List
- it is an IViewTaggerProvider, meaning it is a factory that will created taggers for
  the editor.

 The other key class is the `TextBufferIssueTracker`:
 - the TaggerProvider creates one per file that is opened
 - handles interaction with the daemon to calculate the issues for that file, which are stored in an IssuesSnapshot
 - handles translating the location of issues in the document as it is edited by the user
 - notifies the TaggerProvider when issues have changed, which in turn notifies the Error List
 - notifies IssuesTaggers when issues have changed, which in turn notifies the editor

The other classes are largely notification plumbing:
- SinkManager, SnapshotFactory: plumbing to pass data and notifications to the Error List
- IssueTagger: plumbing to pass data and notifications to the editor
- AnalysisIssueVisualization: data class to associate `IAnalysisIssue` with a location (`SnapshotSpan`) in a text buffer (`ITextSnapshot`)


Editor integration:
-------------------

                              -------- IssueTagger      // flyweight - multiple taggers for each view, multiple views per ITextBuffer
                              |  1..*                   // Signals to the editor when the tags have changed
                              | 
             ------- TextBufferIssueTracker             // Interacts with the daemon and tracks the issues for a single file
             |                |                         // Monitors edits to the live document and translates issue locations
             |                |
             |                |  1..1                 
             |                ------- SnapshotFactory + IssuesSnapshot
             |
             | 1.. *  // one tracker per file i.e. per text buffer
             |
--->   TaggerProvider  // IViewTaggerProvider, singleton


The same document can be opened in multiple separate windows (views) at the same time (by using "Window", "New window", or "Window, "Split").
- all of these views share a single ITextBuffer
- each view has its own set of "taggers"
- a tagger is responsible for adding metadata to a span of text (e.g. "the text in characters 45-52 have an error")
- each view can have more than one tagger per view. Taggers are used by various editor features e.g. the code that
  adds squigglies, or the code that displays tooltips over those squigglies. Each of those editor features will
  request its own tagger from the tagger provider, so we will be creating at least two taggers per view.
  We don't control when the taggers are requested or when they are disposed.
- the tagger is responsible for notifying any listeners when the set of tags has changed.
- the listener will then ask the tagger for the updated set of tags (e.g. so it can redraw the squigglies in the
  correct place)

Error list integration:
-----------------------

---> ISonarErrorListDataSource  // ITableDataSource, singleton
              |
              | 1..*    // many: one per ITableDataSink. In practice, only expecting one, for the Error List.
         ISinkManager   
              |
              | 1..*    // many: registers/unregisters factories with ITableDataSink
              |  
   ITableEntriesSnapshotFactory  // one per TextBufferIssueTracker i.e. per file
       + IssuesSnapshot


- `SinkManager` is used to keep track of subscribers of `TaggerProvider`.
  A `SinkManager` notifies subscribers of changes in errors.

- `SinkManager` gets updates through factories.



Tracking document edits:
------------------------
See MS docs "Inside the editor": https://docs.microsoft.com/en-us/visualstudio/extensibility/inside-the-editor?view=vs-2019

A text file opened in the VS Editor is represented as an ITextBuffer. The buffer can be thought of as series of
immutable ITextSnapshots.  A region of text in a ITextSnapshot is represented by a SnapshotSpan.
Each time the buffer is edited, a new ITextSnapshot is created. The buffer tracks the changes between snapshots,
making it possible to translate from a SnapshotSpan in the previous snapshot to the corresponding SnapshotSpan
in the new ITextSnapshot e.g.
* lines are added at the beginning of the buffer -> all old spans can be translate to spans in the new snapshot.
* lines are added at the end of the buffer -> all old spans can be translated to snaps in the new snapshot.
   The old spans still need to be translated, even though the positions of the span haven't changed.
* a line is deleted from the buffer -> any spans that included positions in the deleted line cannot be translated
   to spans in the new snapshot. All other old spans can be translated to new spans.
 
A SnapshotSpan can be translated across multiple snapshot versions e.g. from snapshot v1 to snapshot v3.

Implementation note: the VS Editor also supports "tracking spans" that work across snapshots i.e. it seems they
automatically translate the span as the buffer changes (so potentially we could get rid of some our "manual"
translation code). The Roslyn tagger uses tracking spans.


A) Processing issues returned by an analyzer
--------------------------------------------
When a file is analysed a list of issues is returned. This contains the error code etc plus the text position data
(start line, end line, start pos and end pos). The text position returned by the analyzer needs to be mapped to a
SnapshotSpan in the current ITextSnapshot. This is what the AnalysisIssueVisualization does - associates an analyser issue with a text span.
From this point on, we are only interested in the SnapshotSpan. The original text position returned by the analyzer is 
no longer relevant.

NOTE: there is a possibility that the buffer could have changed between the analysis started and the resulting issues
being mapped to spans in the ITextSnapshot. Once analyzer issues have been mapped to AnalysisIssueVisualizations against the 
analysis-snapshot, the spans should be translated to spans in the current snapshot. Otherwise, the spans could be
wrong if the document has been edited in the interval between the analysis being triggered and the analysis issues
being processed. See bug #1487: https://github.com/SonarSource/sonarlint-visualstudio/issues/1487

B) Handling edits to the document
---------------------------------
When the buffer is changed (i.e. the document is edited), we need to:
1) translate all of the spans in the AnalysisIssueVisualizations to spans in the ITextSnapshot;
2) tell our tagger it needs to update the squigglies; and
3) tell the Error List it needs to refresh the list of errors for this file.
We need to do (3) so that the Error List shows the up-to-date line and column numbers, but also so that issues on lines
that have been deleted are removed from the Error List.