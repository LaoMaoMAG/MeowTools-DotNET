using LiteDB;

namespace MeowTools;

/// <summary>
/// 配置文件数据库类
/// 本模块基于LitDB实现
/// </summary>
public class ConfigDb
{
	// 配置文件路径
	public string ConfigFilePath { get; }
	// 配置表名称
	public string ConfigName { get; }
	
    // 数据库对象
	public LiteDatabase Data { get; }
	// 配置数据
	private ILiteCollection<ConfigData> ConfigCollection { get; }
	// 数据库连接表
	private static Dictionary<string, LiteDatabase> DbConnTable = new();
	// 数据库名称表
	// private static Dictionary<string, List<string>> DbNameTable = new();

	
	/// <summary>
	/// 配置数据结构
	/// </summary>
	private class ConfigData
	{
		public string? Id { get; set; }
		public BsonValue? Value { get; set; } // 使用BsonValue以支持多种数据类型
	}
	
	
	/// <summary>
	/// 构造方法
	/// </summary>
	public ConfigDb(string configFilePath, string configName)
	{
		// 初始化成员
		ConfigFilePath = configFilePath;
		ConfigName = configName;
		
		// 如果连接不存在创建连接
		if (!DbConnTable.ContainsKey(ConfigFilePath)) DbConnTable[ConfigFilePath] =  new LiteDatabase(ConfigFilePath);
		
		// 连接数据库
		Data = DbConnTable[ConfigFilePath];
		
		// 创建配置表
		ConfigCollection = Data.GetCollection<ConfigData>(ConfigName);
		ConfigCollection.EnsureIndex(x => x.Id, true); // 为Key创建唯一索引以提高查询效率并确保唯一性
	}
	
	
	/// <summary>
	/// 初始化配置数据
	/// </summary>
	/// <param name="key">键值</param>
	/// <param name="value">内容</param>
	/// <typeparam name="T">数据类型</typeparam>
	public void Init<T>(string key, T value)
	{
		if (!IsExist(key)) Set(key, value);
	}
	
	/// <summary>
	/// 设置配置数据
	/// </summary>
	/// <param name="key">键值</param>
	/// <param name="value">内容</param>
	/// <typeparam name="T">数据类型</typeparam>
	public void Set<T>(string key, T value)
	{
		var bsonValue = BsonMapper.Global.Serialize(value);
		var kvPair = new ConfigData { Id = key, Value = bsonValue };
		ConfigCollection.Upsert(kvPair); // 如果存在则更新，不存在则插入
	}
	
	/// <summary>
	/// 获取配置数据
	/// </summary>
	/// <param name="key">键值</param>
	/// <typeparam name="T">数据类型</typeparam>
	/// <returns>配置数据</returns>
	public T? Get<T>(string key)
	{
		var kvPair = ConfigCollection.FindOne(x => x.Id == key);
		if (kvPair == null) return default;
		return BsonMapper.Global.Deserialize<T>(kvPair.Value);
	}

	/// <summary>
	/// 配置是否纯在
	/// </summary>
	/// <param name="key">键值</param>
	/// <returns></returns>
	public bool IsExist(string key)
	{
		return ConfigCollection.Exists(x => x.Id == key);
	}

	/// <summary>
	/// 删除键值
	/// </summary>
	/// <param name="key">键值</param>
	public void Delete(string key)
	{
		var doc = ConfigCollection.FindOne(x => x.Id == key);
		if (doc != null) ConfigCollection.Delete(doc.Id);
	}
}