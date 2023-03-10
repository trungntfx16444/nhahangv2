using PKWebShop.AppLB;

namespace PKWebShop.Services
{
    using System;
    using System.Data;
    using System.Data.Entity;
    using Inner.Libs.Helpful.Infra;
    using Models;
    using ModelsView;

    public abstract class ServicesBase : Disposable
    {
        public WebShopEntities DB
        {
            get
            {
                _db ??= new WebShopEntities();
                return (WebShopEntities)_db;
            }
        }

        protected ServicesBase(DbContext db)
            : base(db)
        {
            _db = db;
        }

        protected ServicesBase(bool trans = false, IsolationLevel _lockLevel = IsolationLevel.ReadUncommitted)
            : base(trans, _lockLevel)
        {
            BeginTransaction(DB, trans, _lockLevel);
        }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
                GC.SuppressFinalize(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public ResponseResult HandleResponse(Action callback)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                return new ResponseResult { Message = ex.ToString() }.ServerError();
            }
            return new ResponseResult().Success();
        }
    }
}