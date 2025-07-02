using SampleApi.CommonUtils.Extensions;
using SampleApi.Models.Enums;
using System.Text.Json.Serialization;

namespace SampleApi.Models.Dtos
{
    public class WorkflowModel
    {
        public class Workflow
        {
            public required string WorkflowType { get; set; }
            public required string OrganizationId { get; set; }
            public required string OrderType { get; set; }
            public Guid Id { get; set; }
            public required IEnumerable<WorkflowNode> Flow { get; set; }
        }

        public class WorkflowNode
        {
            public Guid Id { get; set; }
            public Filter? Filter { get; set; }
            public required string Module { get; set; }
            public Mapper? Mapper { get; set; }
            public IEnumerable<WorkflowRoute>? Routes { get; set; }

            [JsonIgnore]
            public EnumModule EnumModule
            {
                get => Module.ToEnumFromDisplayName<EnumModule>();
            }
        }

        public class WorkflowRoute
        {
            public required IEnumerable<WorkflowNode> Flow { get; set; }
        }

        public class Filter
        {
            public IEnumerable<IEnumerable<Condition>>? Conditions { get; set; }
        }

        public class Condition
        {
            public required string Field { get; set; }
            public required string Value { get; set; }
            public required string Operator { get; set; }

            [JsonIgnore]
            public EnumOperaterType EnumOperater
            {
                get => Operator.ToEnumFromDisplayName<EnumOperaterType>();
            }
        }

        public class Mapper
        {
            public string? Name { get; set; }
            public string? Url { get; set; }
            public string? Method { get; set; }
            public IEnumerable<Variable>? Variables { get; set; }
            public IEnumerable<Variable>? AppendVariables { get; set; }
        }

        public class Variable
        {
            public required string Name { get; set; }
            public required string Value { get; set; }
        }
    }
}
