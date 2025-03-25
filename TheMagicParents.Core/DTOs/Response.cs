using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Models;

namespace TheMagicParents.Core.DTOs
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

        public Response(string message = "", T data = default, Boolean status = false, List<string> errors = null)
        {
            this.Message = message;
            this.Status = status;
            this.Data = data;
            this.Errors = errors;
        }

        public Response(string errorMessage)
        {
            Message = errorMessage;
            Status = false;
            Errors = new List<string> { errorMessage };
        }

        public Boolean Status { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
    }
}
