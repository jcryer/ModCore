﻿using System.Threading.Tasks;

namespace ModCore
{
    internal static class Program
    {
        private static Task Main(string[] args) =>
            new ModCore().InitializeAsync(args);
    }
}
