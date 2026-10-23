using Xunit;
// Each collection owns a real database. Bound concurrent seed/drop checkpoints on local disks.
// Tests of simultaneous business operations still run their explicit parallel requests.
[assembly: CollectionBehavior(MaxParallelThreads=2)]
