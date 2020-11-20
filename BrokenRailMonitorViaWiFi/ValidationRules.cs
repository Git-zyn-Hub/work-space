using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BrokenRail3MonitorViaWiFi
{
    class ValidationRules
    {
    }

    public class ValidationRuleFrom0To255 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 0
                || parseResult > 255)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }
    public class ValidationRuleFrom0To4095 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 0
                || parseResult > 4095)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }


    public class ValidationRuleFrom1To5 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 1
                || parseResult > 5)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }

    public class ValidationRuleFrom1To8 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 1
                || parseResult > 8)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }


    public class ValidationRuleFrom0To65535 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 0
                || parseResult > 65535)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }
}
