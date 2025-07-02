using System.ComponentModel.DataAnnotations;

namespace SampleApi.Models.Enums
{
    public enum EnumModule
    {
        [Display(Name = "ORBIT_API")]
        OrbitApi,

        [Display(Name = "SET_VARIABLES")]
        SetVariables,

        [Display(Name = "ROUTER")]
        Router,
    }
}
