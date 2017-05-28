using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.ECS;
using PMMEditor.Log;

namespace PMMEditor.Models
{
    public class LoggerFilter : Component
    {
        public ILogger Logger { get; set; }
    }
}
