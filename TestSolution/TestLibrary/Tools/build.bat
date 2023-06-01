protoc -I=. --csharp_out=. message.proto

copy Message.cs  ..\messages\Message.cs
pause
