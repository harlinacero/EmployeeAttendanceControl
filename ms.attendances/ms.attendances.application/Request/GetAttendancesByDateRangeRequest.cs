using System.ComponentModel.DataAnnotations;

namespace ms.attendances.application.Request
{
    public class GetAttendancesByDateRangeRequest : IValidatableObject
    {
        public const int MaxRangeDays = 31;

        [Required]
        public string UserName { get; set; }

        [Required]
        public DateOnly? From { get; set; }

        [Required]
        public DateOnly? To { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (From > To)
            {
                yield return new ValidationResult("The start date cannot be later than the end date.", [nameof(From), nameof(To)]);
            }
            else if (To.Value.DayNumber - From.Value.DayNumber + 1 > MaxRangeDays)
            {
                yield return new ValidationResult($"The date range cannot exceed {MaxRangeDays} days.", [nameof(From), nameof(To)]);
            }
        }
    }
}
