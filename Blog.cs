﻿using System;
using System.Collections.Generic;

namespace EntityFrameworkTesting
{
    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }

        public List<Post> Posts { get; } = new();
    }
}
