using System.ComponentModel.DataAnnotations;

namespace SampleApi.Models.Enums
{
    public enum EnumOperaterType
    {
        [Display(Name = "EQUAL_TO")]
        EqualTo,

        [Display(Name = "NOT_EQUAL_TO")]
        NotEqualTo,

        [Display(Name = "CONTAINS")]
        Contains,

        [Display(Name = "DOES_NOT_CONTAIN")]
        DoesNotContain,

        [Display(Name = "STARTS_WITH")]
        StartsWith,

        [Display(Name = "ENDS_WITH")]
        EndsWith,

        [Display(Name = "MATCHES_PATTERN")]
        MatchesPattern,

        [Display(Name = "ARRAY_LENGTH_EQUAL_TO")]
        ArrayLengthEqualTo,

        [Display(Name = "ARRAY_LENGTH_NOT_EQUAL_TO")]
        ArrayLengthNotEqualTo,

        [Display(Name = "ARRAY_LENGTH_GREATER_THAN")]
        ArrayLengthGreaterThan,

        [Display(Name = "ARRAY_LENGTH_LESS_THAN")]
        ArrayLengthLessThan
    }
}
