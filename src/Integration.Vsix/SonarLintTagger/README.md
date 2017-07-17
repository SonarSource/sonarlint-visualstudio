SonarLintTracker
----------------

Important terms in Visual Studio development:

- Tags: text markers in the editor, such as underlinings of errors
- Error List: is the general purpose view for all kinds of errors

In a nutshell:

- `TaggerProvider` gets created when first activated, for example by opening a text document.

- `TaggerProvider` provides not only tags but also errors for Error List.
  As a consequence, Error List automatically subscribes to it.

- `SinkManager` is used to keep track of subscribers of `TaggerProvider`.
  A `SinkManager` notifies subscribers of changes in errors.

- `SinkManager` gets updates through factories.

- The errors change in two ways:
  1. As a result of SonarLint analysis
  2. As a result of editor changes, moving the locations of existing errors

- `TaggerProvider` creates taggers for each buffer when requested.
  It creates one `IssueTracker` per file, to track location changes.

- `IssueTracker` and `SinkManager` react on issue changes triggered by SonarLint analysis
  or editor changes.

The main classes and their purposes:

- `IssueMarker`: track issue with error span (`SnapshotSpan`) in a text buffer (`ITextSnapshot`),
  with a helper method to relocate (translate) itself when the span is moved.

- `IssueTracker`: track issues for a specific buffer. Translate issuer locations
  when they are moved by editor changes in the buffer.
  Create tags from issues in the current snapshot, refreshing only part of the buffer, between first issue and last.

- `SinkManager`: maybe: link `ITableDataSink` with `TaggerProvider`,
  to synchronize the content of the Error List with the tags in the editor.

- `IssuesSnapshot`: provide the content details in the Error List, based on the current snapshot of issues list.

- `SnapshotFactory`: track current issues snapshot, and manage switching to next snapshot.

- `TaggerProvider`: data source for the Error List. Also provide tagger for issues.