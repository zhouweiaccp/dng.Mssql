using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SafeObjectPool
{

    public class DefaultPolicy<T> : IPolicy<T>
    {

        public virtual string Name { get; set; } = typeof(DefaultPolicy<T>).GetType().FullName;

        public virtual int PoolSize { get; set; } = 100;
        /// <summary>
        /// 定时释放30分钟
        /// </summary>
        public virtual int PoolReleaseInterval { get; set; } = 30;
        /// <summary>
        /// 最少连接池5个
        /// </summary>
        public virtual int PoolMinSize { get; set; } = 5;
        public virtual TimeSpan SyncGetTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public virtual TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(50);
        public virtual int AsyncGetCapacity { get; set; } = 10000;
        public virtual bool IsThrowGetTimeoutException { get; set; } = true;
        public virtual int CheckAvailableInterval { get; set; } = 5;
       

        public Func<T> CreateObject;
        public Action<Object<T>> OnGetObject;

        public virtual T OnCreate()
        {
            return CreateObject();
        }

        public virtual void OnDestroy(T obj)
        {

        }

        public virtual void OnGet(Object<T> obj)
        {
            //Console.WriteLine("Get: " + obj);
            OnGetObject?.Invoke(obj);
        }

//        public Task OnGetAsync(Object<T> obj)
//        {
//            //Console.WriteLine("GetAsync: " + obj);
//            OnGetObject?.Invoke(obj);
//#if NET40
//            return Task.Factory.StartNew(()=> true );
//#else
//            return Task.FromResult(true);
//#endif
//        }

        public virtual void OnGetTimeout()
        {

        }

        public virtual  void OnReturn(Object<T> obj)
        {
            //Console.WriteLine("Return: " + obj);
        }

        public virtual bool OnCheckAvailable(Object<T> obj)
        {
            return true;
        }

        public virtual  void OnAvailable()
        {

        }

        public virtual  void OnUnavailable()
        {

        }
    }
}