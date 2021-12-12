// Copyright Matteo Beltrame

using System.Drawing;

namespace NFTGenerator;

internal interface IMediaProvider
{
    public Bitmap ProvideMedia();
}