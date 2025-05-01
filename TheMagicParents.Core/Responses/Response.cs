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
        private IQueryable<User> data;

        public Response(string message, IQueryable<User> data, bool status)
        {
            Message = message;
            this.data = data;
            Status = status;
        }

        public Response(string message = "", T data = default, bool status = false, List<string> errors = null)
        {
            Message = message;
            Status = status;
            Data = data;
            Errors = errors;
        }

        public Response(string errorMessage)
        {
            Message = errorMessage;
            Status = false;
            Errors = new List<string> { errorMessage };
        }

        public bool Status { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
    }
}
