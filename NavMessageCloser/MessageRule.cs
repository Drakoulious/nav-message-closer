using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavMessageCloser
{
    class MessageRule
    {
        public string message { get; set; }
        public bool onConfirm { get; set; }
        public bool closeMessage { get; set; }
        public string startProgramm { get; set; }
    }
}
