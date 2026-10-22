using ms.attendances.application.Request;
using System.ComponentModel.DataAnnotations;

namespace ms.attendances.application.test.Request
{
    public class GetAttendancesByDateRangeRequestTests
    {
        private static List<ValidationResult> Validate(GetAttendancesByDateRangeRequest request)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void ValidateShouldPassWhenRangeIsValid()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 3, 1), To = new DateOnly(2026, 3, 31) };

            // Act
            var results = Validate(request);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void ValidateShouldPassWhenFromEqualsTo()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 3, 10), To = new DateOnly(2026, 3, 10) };

            // Act
            var results = Validate(request);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void ValidateShouldPassWhenRangeIsExactly31Days()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 1, 31) };

            // Act
            var results = Validate(request);

            // Assert
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateShouldFailWhenUserNameIsMissing(string? userName)
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = userName, From = new DateOnly(2026, 3, 1), To = new DateOnly(2026, 3, 31) };

            // Act
            var results = Validate(request);

            // Assert
            var error = Assert.Single(results);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.UserName), error.MemberNames);
        }

        [Fact]
        public void ValidateShouldFailWhenFromIsMissing()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = null, To = new DateOnly(2026, 3, 31) };

            // Act
            var results = Validate(request);

            // Assert
            var error = Assert.Single(results);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.From), error.MemberNames);
        }

        [Fact]
        public void ValidateShouldFailWhenToIsMissing()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 3, 1), To = null };

            // Act
            var results = Validate(request);

            // Assert
            var error = Assert.Single(results);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.To), error.MemberNames);
        }

        [Fact]
        public void ValidateShouldFailWhenFromIsLaterThanTo()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 3, 10), To = new DateOnly(2026, 3, 1) };

            // Act
            var results = Validate(request);

            // Assert
            var error = Assert.Single(results);
            Assert.Equal("The start date cannot be later than the end date.", error.ErrorMessage);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.From), error.MemberNames);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.To), error.MemberNames);
        }

        [Fact]
        public void ValidateShouldFailWhenRangeExceeds31Days()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 2, 1) };

            // Act
            var results = Validate(request);

            // Assert
            var error = Assert.Single(results);
            Assert.Equal("The date range cannot exceed 31 days.", error.ErrorMessage);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.From), error.MemberNames);
            Assert.Contains(nameof(GetAttendancesByDateRangeRequest.To), error.MemberNames);
        }
    }
}
