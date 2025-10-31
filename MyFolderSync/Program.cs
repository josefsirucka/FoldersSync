// <copyright file="Program.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 23.10 2025</summary>

#if DEBUG
//Console.WriteLine("Debug mode is ON. Waiting for debugger to attach...");
//Console.ReadKey();
#endif

MyFolderSyncApp app = new(args);
await app.Run();
