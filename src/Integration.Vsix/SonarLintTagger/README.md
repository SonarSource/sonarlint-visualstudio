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
- it is an ITableDataSource, meaning it can provide issues to be shown in the Error List
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
- IssueMarker: data class to associate a SonarQube `Issue` with a location (`SnapshotSpan`) in a text buffer (`ITextSnapshot`)


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

--->   TaggerProvider  // ITableDataSource, singleton)
              |
              | 1..*    // many: one per ITableDataSink. In practice, only expecting one, for the Error List.
          SinkManger   
              |
              | 1..*    // many: registers/unregisters factories with ITableDataSink
              |  
       SnapshotFactory  // one per TextBufferIssueTracker i.e. per file
       + IssuesSnapshot


- `SinkManager` is used to keep track of subscribers of `TaggerProvider`.
  A `SinkManager` notifies subscribers of changes in errors.

- `SinkManager` gets updates through factories.


