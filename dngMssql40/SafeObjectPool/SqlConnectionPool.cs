using SafeObjectPool;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System.Data.SqlClient {

	/// <summary>
	/// var pool = new System.Data.SqlClient.SqlConnectionPool("名称", connectionString, 可用时触发的委托, 不可用时触发的委托);
//	var conn = pool.Get();

//try {
//	// 使用 ...
//	pool.Return(conn); //正常归还
//} catch (Exception ex)
//{
//	pool.Return(conn, ex); //发生错误时归还
//}
/// </summary>
public class SqlConnectionPool : ObjectPool<SqlConnection> {

		internal Action availableHandler;
		internal Action unavailableHandler;

		public SqlConnectionPool(string name, string connectionString, Action availableHandler, Action unavailableHandler) : base(null) {
			var policy = new SqlConnectionPoolPolicy {
				_pool = this,
				Name = name
			};
			this.Policy = policy;
			policy.ConnectionString = connectionString;

			this.availableHandler = availableHandler;
			this.unavailableHandler = unavailableHandler;
		}

		public void Return(Object<SqlConnection> obj, Exception exception, bool isRecreate = false) {
			if (exception != null && exception is SqlException) {

				if (obj.Value.Ping() == false) {

					base.SetUnavailable(exception);
				}
			}
			base.Return(obj, isRecreate);
		}
	}

	public class SqlConnectionPoolPolicy : DefaultPolicy<SqlConnection> {

		internal SqlConnectionPool _pool;
		public  override string Name { get; set; } = "SQLServer SqlConnection 对象池";
		public override int PoolSize { get; set; } = 100;
		public override TimeSpan SyncGetTimeout { get; set; } = TimeSpan.FromSeconds(10);
		public override TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;
		public override int AsyncGetCapacity { get; set; } = 10000;
		public override bool IsThrowGetTimeoutException { get; set; } = true;
		/// <summary>
		/// 5秒
		/// </summary>
		public override int CheckAvailableInterval { get; set; } = 5;

		private string _connectionString;
		/// <summary>
		/// Server=192.168.253.125;Database=EDoc;uid=sa;pwd=1qaz2WSX;Min Pool Size=10;Max Pool Size=100;Pooling=True
		/// </summary>
		public string ConnectionString {
			get => _connectionString;
			set {
				_connectionString = value ?? "";
				Match m = Regex.Match(_connectionString, @"Max\s*pool\s*size\s*=\s*(\d+)", RegexOptions.IgnoreCase);
				if (m.Success == false || int.TryParse(m.Groups[1].Value, out var poolsize) == false || poolsize <= 0) poolsize = 100;
				PoolSize = poolsize;

				var initConns = new Object<SqlConnection>[poolsize];
				for (var a = 0; a < poolsize; a++) try { initConns[a] = _pool.Get(); } catch { }
				foreach (var conn in initConns) _pool.Return(conn);
			}
		}

        //public int PoolReleaseInterval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public int PoolMinSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
        public override bool OnCheckAvailable(Object<SqlConnection> obj) {
			if (obj.Value.State == ConnectionState.Closed) obj.Value.Open();
			var cmd = obj.Value.CreateCommand();
			cmd.CommandText = "select 1";
			cmd.ExecuteNonQuery();
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual SqlConnection OnCreate() {
			var conn = new SqlConnection(_connectionString);
			return conn;
		}

		public  override void OnDestroy(SqlConnection obj) {
			if(obj==null){return;}
			if (obj.State != ConnectionState.Closed) obj.Close();
			obj.Dispose();
		}

		public override void OnGet(Object<SqlConnection> obj) {

			if (_pool.IsAvailable) {

				if (obj.Value.State != ConnectionState.Open || DateTime.Now.Subtract(obj.LastReturnTime).TotalSeconds > 60 && obj.Value.Ping() == false) {

					try {
						obj.Value.Open();
					} catch (Exception ex) {
						if (_pool.SetUnavailable(ex) == true)
							throw new Exception($"【{this.Name}】状态不可用，等待后台检查程序恢复方可使用。{ex.Message}");
					}
				}
			}
		}

		//async public Task OnGetAsync(Object<SqlConnection> obj) {

		//	if (_pool.IsAvailable) {

		//		if (obj.Value.State != ConnectionState.Open || DateTime.Now.Subtract(obj.LastReturnTime).TotalSeconds > 60 && obj.Value.Ping() == false) {

		//			try {
		//				await obj.Value.OpenAsync();
		//			} catch (Exception ex) {
		//				if (_pool.SetUnavailable(ex) == true)
		//					throw new Exception($"【{this.Name}】状态不可用，等待后台检查程序恢复方可使用。{ex.Message}");
		//			}
		//		}
		//	}
		//}

		//public void OnGetTimeout() {

		//}

		public override void OnReturn(Object<SqlConnection> obj) {
			if (obj.Value.State != ConnectionState.Closed) try { obj.Value.Close(); } catch { }
		}

		public override void OnAvailable() {
			_pool.availableHandler?.Invoke();
		}

		public override void OnUnavailable() {
			_pool.unavailableHandler?.Invoke();
		}
	}

	public static class SqlConnectionExtensions {

		public static bool Ping(this SqlConnection that) {
			try {
				var cmd = that.CreateCommand();
				cmd.CommandText = "select 1";
				cmd.ExecuteNonQuery();
				return true;
			} catch {
				if (that.State != ConnectionState.Closed) try { that.Close(); } catch { }
				return false;
			}
		}
	}
}
