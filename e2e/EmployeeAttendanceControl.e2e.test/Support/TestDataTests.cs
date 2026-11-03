namespace EmployeeAttendanceControl.e2e.test.Support
{
    public class TestDataTests
    {
        [Fact]
        public void NewUserNameShouldContainRunIdAndTag()
        {
            // Act
            var userName = TestData.NewUserName("flow");

            // Assert
            Assert.StartsWith($"e2e-{TestData.RunId}-", userName);
            Assert.Contains("flow", userName);
            Assert.Matches(@"^\d{14}-[0-9a-f]{6}$", TestData.RunId);
        }

        [Fact]
        public void NewUserNameShouldBeUniqueForTheSameTag()
        {
            // Act
            var first = TestData.NewUserName("flow");
            var second = TestData.NewUserName("flow");

            // Assert
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void NewUserNameShouldBeLowerCase()
        {
            // Act
            var userName = TestData.NewUserName("MixedCase");

            // Assert
            Assert.Equal(userName.ToLowerInvariant(), userName);
        }
    }
}
