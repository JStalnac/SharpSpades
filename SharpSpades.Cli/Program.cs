﻿using ENet.Managed;
using SharpSpades.Net.Packets;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpSpades.Cli
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var server = new Server(Directory.GetCurrentDirectory());

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    server.StopAsync().Wait();
                };

                await server.StartAsync();
            }
            finally
            {
                ManagedENet.Shutdown();
            }
        }
    }
}