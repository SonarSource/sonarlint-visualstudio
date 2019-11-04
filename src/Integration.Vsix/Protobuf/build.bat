set toolspath=%1
%toolspath%\protoc.exe -I=. --csharp_out . --grpc_out . sonarlint-daemon.proto --plugin=protoc-gen-grpc=%toolspath%\grpc_csharp_plugin.exe
