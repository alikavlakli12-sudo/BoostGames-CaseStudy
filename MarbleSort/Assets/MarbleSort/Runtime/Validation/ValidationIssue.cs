using System.Collections.Generic;

namespace MarbleSort.Validation
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class ValidationIssue
    {
        public ValidationIssue(ValidationSeverity severity, string code, string context, string message)
        {
            Severity = severity;
            Code = code;
            Context = context;
            Message = message;
        }

        public ValidationSeverity Severity { get; }

        public string Code { get; }

        public string Context { get; }

        public string Message { get; }

        public override string ToString()
        {
            return $"[{Severity}] {Code} ({Context}): {Message}";
        }
    }

    public sealed class ValidationReport
    {
        private readonly List<ValidationIssue> issues = new List<ValidationIssue>();

        public IReadOnlyList<ValidationIssue> Issues => issues;

        public bool HasErrors
        {
            get
            {
                for (int index = 0; index < issues.Count; index++)
                {
                    if (issues[index].Severity == ValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal void Add(ValidationSeverity severity, string code, string context, string message)
        {
            issues.Add(new ValidationIssue(severity, code, context, message));
        }
    }
}
