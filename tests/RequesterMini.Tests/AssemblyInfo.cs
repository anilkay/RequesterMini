// ReactiveUI's MessageBus is a process-wide singleton and these ViewModels both publish and listen
// on it, so tests running side by side would deliver each other's messages.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
