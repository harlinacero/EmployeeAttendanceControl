using EmployeeAttendanceControl.e2e.test.Support;
using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;

namespace EmployeeAttendanceControl.e2e.test.Flows
{
    [Collection(E2ECollection.Name)]
    public class AttendanceFlowTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public AttendanceFlowTests(E2EEnvironmentFixture environment) => _environment = environment;

        // A run near midnight UTC can mark attendance on one day and check it on the next, hence the +/- 1 day below
        private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

        [Fact]
        public async Task MarkPresentShouldAddRecordToHistory()
        {
            // Arrange
            var employee = await _environment.CreateUsableEmployeeAsync("present");

            // Act
            await _environment.Client.MarkAttendanceAsync(employee.Token, true, "on site");
            var history = await WaitForHistoryAsync(employee, 1);

            // Assert
            var record = Assert.Single(history);
            Assert.True(record.Attendance);
            Assert.Equal(employee.UserName, record.UserName);
            Assert.Equal(employee.FullName, record.FullName);
            Assert.Equal("on site [0] Asistencias", record.Notes);
            Assert.InRange(DateOnly.FromDateTime(record.Date), Today.AddDays(-1), Today.AddDays(1));
        }

        [Fact]
        public async Task MarkAbsentShouldAppendWithoutChangingPreviousRecord()
        {
            // Arrange
            var employee = await _environment.CreateUsableEmployeeAsync("absent");
            await _environment.Client.MarkAttendanceAsync(employee.Token, true, "on site");
            var first = Assert.Single(await WaitForHistoryAsync(employee, 1));

            // Act
            await _environment.Client.MarkAttendanceAsync(employee.Token, false, "day off");
            var history = await WaitForHistoryAsync(employee, 2);

            // Assert
            Assert.Equal(2, history.Count);
            Assert.Equal(first, history.Single(record => record.Id == first.Id));
            var second = history.Single(record => record.Id != first.Id);
            Assert.False(second.Attendance);
            Assert.Equal("day off [1] Asistencias", second.Notes);
        }

        [Fact]
        public async Task ByDateRangeShouldReturnRecordsOnlyWhenRangeIncludesToday()
        {
            // Arrange
            var employee = await _environment.CreateUsableEmployeeAsync("range");
            await _environment.Client.MarkAttendanceAsync(employee.Token, true, "on site");
            await WaitForHistoryAsync(employee, 1);
            await _environment.Client.MarkAttendanceAsync(employee.Token, false, "day off");
            await WaitForHistoryAsync(employee, 2);

            // Act
            var includingToday = await _environment.Client.GetAttendancesByDateRangeAsync(_environment.AdminToken, employee.UserName, Today.AddDays(-1), Today.AddDays(1));
            var endedBefore = await _environment.Client.GetAttendancesByDateRangeAsync(_environment.AdminToken, employee.UserName, Today.AddDays(-40), Today.AddDays(-35));

            // Assert
            Assert.Equal(2, includingToday.Count);
            Assert.Empty(endedBefore);
        }

        [Theory]
        [InlineData(1, 0)]   // The start is after the end
        [InlineData(-31, 0)] // 32 days, the limit is 31
        public async Task ByDateRangeShouldRejectInvalidRange(int fromOffset, int toOffset)
        {
            // Arrange
            var path = GatewayClient.ByDateRangePath(E2ESettings.AdminUser, Today.AddDays(fromOffset), Today.AddDays(toOffset));

            // Act
            var response = await _environment.Client.SendAsync(HttpMethod.Get, path, _environment.AdminToken);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"GET {path} as admin: expected 400, got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task AddAttendanceShouldAppearInOwnHistory()
        {
            // Arrange
            var employee = await _environment.CreateUsableEmployeeAsync("add");
            var added = new CreateAttendanceDto(employee.FullName, DateTime.UtcNow, true, "added directly");

            // Act
            await _environment.Client.AddAttendanceAsync(employee.Token, added);
            var history = await _environment.Client.GetAttendancesAsync(employee.Token, employee.UserName);

            // Assert
            var record = Assert.Single(history);
            Assert.Equal(employee.UserName, record.UserName);
            Assert.Equal(added.Notes, record.Notes);
        }

        private Task<List<AttendanceDto>> WaitForHistoryAsync(TestEmployee employee, int records)
            => _environment.WaitUntilAsync(
                () => _environment.Client.GetAttendancesAsync(employee.Token, employee.UserName),
                history => history.Count >= records,
                $"Record {records} of the attendance history of '{employee.UserName}' (attendance change reaching the history)");
    }
}
