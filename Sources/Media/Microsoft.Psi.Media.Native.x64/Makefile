FFMPEGLibDir=$(FFMPEGDir)
FFMpegLibs=$(FFMPEGLibDir)/libavdevice/libavdevice.so\
	$(FFMPEGLibDir)/libavfilter/libavfilter.so\
	$(FFMPEGLibDir)/libswresample/libswresample.so\
	$(FFMPEGLibDir)/libavcodec/libavcodec.so\
	$(FFMPEGLibDir)/libavformat/libavformat.so\
	$(FFMPEGLibDir)/libavutil/libavutil.so\
	$(FFMPEGLibDir)/libswscale/libswscale.so
FFMpegIncludes=$(FFMPEGDir)
FFMpegDefines=-DUSE_FFMPEG -DLINUX
SOURCES=\
	FFMPEGReaderNative.o

Microsoft.Psi.Media.Native.so: $(SOURCES)
	g++ -g -shared -Wl,-Bsymbolic -o $@ $(SOURCES) $(FFMpegLibs)

%.o: %.cpp
	g++ -g -fPIC -pthread -std=c++11 -c -o $@ $< -I$(FFMpegIncludes)  $(FFMpegDefines) -Wno-deprecated-declarations

clean:
	rm $(SOURCES)
