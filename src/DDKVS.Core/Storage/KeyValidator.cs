using System;
using System.Text.RegularExpressions;

namespace DDKVS.Core.Storage
{
    public class KeyValidator : IKeyValidator
    {
        private static readonly Regex ValidationExpression = new Regex(@"^[a-z0-9\-_]+$");
        public bool IsValid(string key)
        {
            return ValidationExpression.IsMatch(key);
        }

        public void Validate(string key)
        {
            if (!IsValid(key))
            {
                throw new ArgumentException(
                    "Keys can only consist of alpha numeric characters, dash (-), or underscore (_), and must be lower case.", nameof(key));
            }
        }
    }
}