# Roadmap

This document describes the roadmap for coverlet showing priorities.  
Maintain coverlet means like any other project two things, answer/resolve soon as possible new issues that are blocking our users an second improve product with new features and enhancements in different areas.  
All coverlet issues are labeled and categorized to better support this activites.

As soon as an issue is open is labeled with `untriaged` if not immediately solvable(we need to do some debugging/PoC to understand where is the issue).  
After triage a final correct label is applied and will be taken into account depending on priority order.  
We use `needs more info` if we're waiting for answers.

Default priority order "should" be:

1) Bugs: we should fix bugs as soon as possible and for first bugs related to coverage because this is the goal of coverlet.

Coverage bugs: https://github.com/coverlet-coverage/coverlet/issues?q=is%3Aissue+is%3Aopen+label%3Abug+label%3Atenet-coverage  
Other bugs: https://github.com/coverlet-coverage/coverlet/issues?q=is%3Aissue+is%3Aopen+label%3Abug

2) New features: analyze and add new features, we have three drivers so features could be related to one of these.

Feature requests: https://github.com/coverlet-coverage/coverlet/issues?q=is%3Aissue+is%3Aopen+label%3Afeature-request

3) Performance: we never worked on performance aspect of coverlet, it makes sense for a "new project with some hope", but today coverlet is the facto the dotnet coverage tool, so we HAVE TO approach this aspect.

Performance issues: https://github.com/coverlet-coverage/coverlet/issues?q=is%3Aissue+is%3Aopen+label%3Atenet-performance

Some new features have got a `Discussion` label if we don't have and agreement yet on semantics.  
Discussions: https://github.com/coverlet-coverage/coverlet/issues?q=is%3Aissue+is%3Aopen+label%3Adiscussion

## New features roadmap

This is the list of features we should develop soon as possible:

### High priority

- Allow merge reports solution wide on all flavours  https://github.com/coverlet-coverage/coverlet/issues/662 https://github.com/coverlet-coverage/coverlet/issues/357

- Some perf improvements https://github.com/coverlet-coverage/coverlet/issues/836

### Low priority

- Rethink hits reports strategy https://github.com/coverlet-coverage/coverlet/issues/808 

## Maintainers discussion channel

[As maintainers we should try to find a way to keep in synch, we could use a chat where we can "take note" of progress and if possible answer to questions/doubt, I know this is OSS, but it's hard keep project at high level without "more ideas".]


