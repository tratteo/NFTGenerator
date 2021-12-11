// Copyright Matteo Beltrame

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFTGenerator;

internal interface IMediaProvider
{
    public Bitmap ProvideMedia(); 
}
