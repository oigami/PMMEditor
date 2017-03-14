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
        private static readonly string source =
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

        private static readonly Type EvalType;

        static ExpressionConverter()
        {
            var provider = new JScriptCodeProvider();
            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, source);
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
            EvalType = assembly.GetType("TestPackage.Test");
        }

        public object Convert(object value, Type t, object parameter, CultureInfo culture)
        {
            double v = value as int? ?? (double) value;

            string exp = "var x=" + v + ";" + (string) parameter;


            object evaluator = Activator.CreateInstance(EvalType);

            string output = (string) EvalType.InvokeMember("Eval", BindingFlags.InvokeMethod, null, evaluator,
                                                           new object[] { exp });

            return output;
        }

        public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(nameof(ExpressionConverter));
        }
    }
}
