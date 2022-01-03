// Copyright Matteo Beltrame

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFTGenerator.Source;

public interface IIdOwner
{
    public int Id { get; }
}