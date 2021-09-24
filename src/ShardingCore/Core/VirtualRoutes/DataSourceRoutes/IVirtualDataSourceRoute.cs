using System;
using System.Collections.Generic;
using System.Linq;
using ShardingCore.Core.VirtualDatabase.VirtualDataSources.PhysicDataSources;
using ShardingCore.Sharding.PaginationConfigurations;

namespace ShardingCore.Core.VirtualRoutes.DataSourceRoutes
{
/*
* @Author: xjm
* @Description:
* @Date: Friday, 05 February 2021 13:03:58
* @Email: 326308290@qq.com
*/
    public interface IVirtualDataSourceRoute
    {
        Type ShardingEntityType { get;}
        /// <summary>
        /// 分页配置
        /// </summary>
        PaginationMetadata PaginationMetadata { get; }
        /// <summary>
        /// 是否启用分页配置
        /// </summary>
        bool EnablePagination { get; }
        string ShardingKeyToDataSourceName(object shardingKeyValue);

        /// <summary>
        /// 根据查询条件路由返回物理数据源
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="isQuery"></param>
        /// <returns>data source name</returns>
        List<string> RouteWithPredicate(IQueryable queryable, bool isQuery);

        /// <summary>
        /// 根据值进行路由
        /// </summary>
        /// <param name="shardingKeyValue"></param>
        /// <returns>data source name</returns>
        string RouteWithValue(object shardingKeyValue);

        List<string> GetAllDataSourceNames();
        /// <summary>
        /// 添加数据源
        /// </summary>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        bool AddDataSourceName(string dataSourceName);
        /// <summary>
        /// 初始化
        /// </summary>
        void Init();

    }
    
    public interface IVirtualDataSourceRoute<T> : IVirtualDataSourceRoute where T : class, IShardingDataSource
    {
        /// <summary>
        /// 返回null就是表示不开启分页配置
        /// </summary>
        /// <returns></returns>
        IPaginationConfiguration<T> CreatePaginationConfiguration();
    }
}