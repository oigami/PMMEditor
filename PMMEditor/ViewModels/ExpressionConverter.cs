using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using Microsoft.JScript;

namespace PMMEditor.ViewModels
{
    public class ExpressionConverter : IValueConverter
    {
        private static readonly string _source =
            @"package TestPackage
        {
            class Test
            {
                public function Eval(expression : String) : String
                {
                    return eval(expression);
                }
            }
        }";

        private static readonly Type _evalType;

        static ExpressionConverter()
        {
            var provider = new JScriptCodeProvider();
            var parameters = new CompilerParameters
            {
                GenerateInMemory = true
            };
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, _source);
            if (results.Errors.Count > 0)
            {
                string tmp = "コンパイルエラー\n\n";
                foreach (CompilerError errors in results.Errors)
                {
                    tmp += errors + "\n";
                }
                Console.WriteLine(tmp);
            }
            Assembly assembly = results.CompiledAssembly;
            _evalType = assembly.GetType("TestPackage.Test");
        }

        public object Convert(object value, Type t, object parameter, CultureInfo culture)
        {
            double v = value as int? ?? (double) value;

            string exp = "var x=" + v + ";" + (string) parameter;


            object evaluator = Activator.CreateInstance(_evalType);

            return (string) _evalType.InvokeMember("Eval", BindingFlags.InvokeMethod, null, evaluator,
                                                           new object[] { exp });
        }

        public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(nameof(ExpressionConverter));
        }
    }
}
