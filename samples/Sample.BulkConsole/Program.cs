﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.BulkConsole.DbContexts;
using Sample.BulkConsole.Entities;
using ShardingCore;
using ShardingCore.Core.PhysicTables;
using ShardingCore.Core.VirtualDatabase.VirtualTables;
using ShardingCore.Extensions;
using ShardingCore.TableCreator;

namespace Sample.BulkConsole
{
    class Program
    {
        public static readonly ILoggerFactory efLogger = LoggerFactory.Create(builder =>
        {
            builder.AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
        });
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddShardingDbContext<MyShardingDbContext, MyDbContext>(
                o => o.UseSqlServer("Data Source=localhost;Initial Catalog=MyOrderSharding;Integrated Security=True;"))
                .Begin(o =>
                {
                    o.CreateShardingTableOnStart = true;
                    o.EnsureCreatedWithOutShardingTable = true;
                })
                .AddShardingQuery((conStr, builder) => builder.UseSqlServer(conStr).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking))
                .AddShardingTransaction((connection, builder) => builder.UseSqlServer(connection))
                .AddDefaultDataSource("ds0", "Data Source=localhost;Initial Catalog=MyOrderSharding;Integrated Security=True;")
                .AddShardingTableRoute(op=> {
                    op.AddShardingTableRoute<OrderVirtualRoute>();
                }).End();
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<IShardingBootstrapper>().Start();
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var myShardingDbContext = serviceScope.ServiceProvider.GetService<MyShardingDbContext>();

                var virtualTableManager = serviceScope.ServiceProvider.GetService<IVirtualTableManager<MyShardingDbContext>>();
                var virtualTable = virtualTableManager.GetVirtualTable(typeof(Order));
                if (virtualTable == null)
                {
                    return;
                }
                var now1 = DateTime.Now.Date.AddDays(2);
                var tail = virtualTable.GetVirtualRoute().ShardingKeyToTail(now1);
                try
                {
                    virtualTableManager.AddPhysicTable(virtualTable, new DefaultPhysicTable(virtualTable, tail));
                    var tableCreator = serviceProvider.GetService< IShardingTableCreator < MyShardingDbContext >> ();
                    tableCreator.CreateTable("ds0", typeof(Order), tail);
                }
                catch (Exception e)
                {
                    //ignore
                }
                if (!myShardingDbContext.Set<Order>().Any())
                {
                    var begin = DateTime.Now.Date.AddDays(-3);
                    var now = DateTime.Now;
                    var current = begin;
                    ICollection<Order> orders = new LinkedList<Order>();
                    int i = 0;
                    while (current < now)
                    {
                        orders.Add(new Order()
                        {
                            Id = i.ToString(),
                            OrderNo = $"orderno-" + i.ToString(),
                            Seq = i,
                            CreateTime = current
                        });
                        i++;
                        current = current.AddMilliseconds(100);
                    }

                    var startNew = Stopwatch.StartNew();
                    var bulkShardingEnumerable = myShardingDbContext.BulkShardingTableEnumerable(orders);
                    startNew.Stop();
                    Console.WriteLine($"订单总数:{i}条,myShardingDbContext.BulkShardingEnumerable(orders)用时:{startNew.ElapsedMilliseconds}毫秒");
                    startNew.Restart();
                    foreach (var dataSourceMap in bulkShardingEnumerable)
                    {
                        dataSourceMap.Key.BulkInsert(dataSourceMap.Value.ToList());
                    }
                    startNew.Stop();
                    Console.WriteLine($"订单总数:{i}条,myShardingDbContext.BulkInsert(orders)用时:{startNew.ElapsedMilliseconds}毫秒");

                    Console.WriteLine("ok");
                }

                var b = DateTime.Now.Date.AddDays(-3);
                var queryable = myShardingDbContext.Set<Order>().Where(o => o.CreateTime >= b).OrderBy(o => o.CreateTime);
                var startNew1 = Stopwatch.StartNew();

                int skip = 0, take = 10;
                for (int i = 20000; i < 30000; i++)
                {
                    skip = take * i;
                    startNew1.Restart();
                    var shardingPagedResult = queryable.ToShardingPage(i+1, take);
                    startNew1.Stop();
                        Console.WriteLine($"流式分页skip:[{skip}],take:[{take}]耗时用时:{startNew1.ElapsedMilliseconds}毫秒");
                }

                Console.WriteLine("ok");

            }
        }
    }
}