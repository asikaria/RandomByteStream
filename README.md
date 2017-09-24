# RandomByteStream

Generates random numbers for creating a high-performance test stream.This stream pre-generates 
a few megabytes of random data, and uses that same data repeatedly. This is not suitable for 
security purposes. However, for testing data processing pipelines,this provides just what is 
needed - random (non-compressible) bytes of arbitrary length, available at very high speed.

To use: 
Import from NuGet: Widgeteer.RandomByteStream

In code, use namespace:
  using RandomByteStream;
  
Instantiate the stream:
  Stream s = new RandomByteStream();
  
Read from the Stream, as usual:
  int numbytes = s.Read(buffer, offset, count);

