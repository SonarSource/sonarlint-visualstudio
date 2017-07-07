set toolspath=..\..\..\packages\Grpc.Tools.1.4.1\tools\windows_x64
%toolspath%\protoc.exe -I=. --csharp_out . --grpc_out . sonarlint-daemon.proto --plugin=protoc-gen-grpc=%toolspath%\grpc_csharp_plugin.exe
