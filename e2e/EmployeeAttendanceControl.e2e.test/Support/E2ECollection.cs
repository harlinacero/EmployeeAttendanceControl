namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// Test classes that call the stack join this collection, so they run one at a time and share one fixture.
    /// Classes that don't need the stack stay out of it, otherwise they would start the fixture.
    /// </summary>
    [CollectionDefinition(Name)]
    public class E2ECollection : ICollectionFixture<E2EEnvironmentFixture>
    {
        public const string Name = "E2E";
    }
}
