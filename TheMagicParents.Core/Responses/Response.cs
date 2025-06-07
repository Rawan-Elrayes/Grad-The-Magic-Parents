using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Responses
{
    public class Response<T>
    {
        public Response(string message, T data, int status)
        {
            Message = message;
            Data = data;
            Status = status;
        }

        public Response(string message = "", T data = default, int status = 1, List<string> errors = null)
        {
            Message = message;
            Status = status;
            Data = data;
            Errors = errors;
        }

        public Response(string errorMessage)
        {
            Message = errorMessage;
            Status = 1; // 1 معناها في خطأ
            Errors = new List<string> { errorMessage };
        }

        public int Status { get; set; } // تم تغييره من bool إلى int
        public T Data { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
    }
}