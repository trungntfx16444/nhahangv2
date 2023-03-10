namespace PKWebShop.ModelsView
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Web.Mvc;
    using Inner.Libs.Helpful;
    using Newtonsoft.Json;
    using PKWebShop.Utils;

    public class ResponseResult
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public string Message { get; set; }

        public Dictionary<string, string> MessageDetail { get; set; } = new Dictionary<string, string>();

        public object Records { get; set; }

        public object Record { get; set; }

        public string Response { get; set; }

        public ResponseResult()
        {
        }

        public ResponseResult(HttpStatusCode status)
        {
            Status = status;
        }

        public ResponseResult(string message)
        {
            Message = message;
        }

        public ResponseResult(Exception e)
        {
            Message = e.ToString();
            MessageDetail = e.InnerException != null ? new Dictionary<string, string> { { "Error", e.InnerException?.ToString() ?? string.Empty } } : null;
        }

        public ResponseResult Success()
        {
            Status = HttpStatusCode.OK;
            return this;
        }

        public ResponseResult Error()
        {
            Status = HttpStatusCode.BadRequest;
            return this;
        }

        public ResponseResult ServerError()
        {
            Status = HttpStatusCode.InternalServerError;
            return this;
        }

        public ResponseResult SetData(object data)
        {
            Records = data;
            return this;
        }

        public ResponseResult SetRecord(object record)
        {
            Record = record;
            return this;
        }

        public JsonResult ToJson()
        {
            return new Func.JsonStatusResult(ToObject(), Status);
        }

        public JsonResult ToJson(string customDatetimeFormat)
        {
            return new Func.JsonStatusResult(ToObject(), Status).DateTimeSerialize(customDatetimeFormat);
        }

        public Dictionary<string, object> ToObject()
        {
            Dictionary<string, object> rs = new ()
            {
                { "status", Status },
            };
            if (Status >= HttpStatusCode.BadRequest)
            {
                if (Message != null)
                {
                    rs["message"] = Message;
                }

                if (MessageDetail != null && MessageDetail.Keys.Count > 0)
                {
                    rs["message_detail"] = MessageDetail;
                }
            }
            else
            {
                if (Records != null)
                {
                    rs["records"] = Records;
                }

                if (Record != null)
                {
                    rs["record"] = Record;
                }

                if (Response != null)
                {
                    rs["response"] = Response;
                }
            }
            return rs;
        }
    }
}