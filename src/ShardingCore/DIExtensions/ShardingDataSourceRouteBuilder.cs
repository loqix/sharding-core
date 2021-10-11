﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;

namespace ShardingCore.DIExtensions
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/9/23 9:08:29
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    public class ShardingDataSourceRouteBuilder<TShardingDbContext> : ShardingReadWriteSeparationBuilder<TShardingDbContext>
        where TShardingDbContext : DbContext, IShardingDbContext
    {
        private readonly ShardingCoreConfigBuilder<TShardingDbContext> _shardingCoreConfigBuilder;

        public ShardingDataSourceRouteBuilder(ShardingCoreConfigBuilder<TShardingDbContext> shardingCoreConfigBuilder) : base(shardingCoreConfigBuilder)
        {
            _shardingCoreConfigBuilder = shardingCoreConfigBuilder;
        }

        public ShardingTableBuilder<TShardingDbContext> AddShardingDataSourceRoute(Action<ShardingDatabaseOptions> shardingDatabaseConfigure)
        {

            var shardingDatabaseOptions = new ShardingDatabaseOptions();
            shardingDatabaseConfigure.Invoke(shardingDatabaseOptions);
            var shardingDatabaseRoutes = shardingDatabaseOptions.GetShardingDatabaseRoutes();
            foreach (var shardingDatabaseRoute in shardingDatabaseRoutes)
            {
                _shardingCoreConfigBuilder.ShardingConfigOption.AddShardingDataSourceRoute(shardingDatabaseRoute);
            }

            return new ShardingTableBuilder<TShardingDbContext>(_shardingCoreConfigBuilder);
        }
    }
}
