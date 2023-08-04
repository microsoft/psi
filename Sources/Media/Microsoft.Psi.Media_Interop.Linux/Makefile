FFMPEGLibDir=$(FFMPEGDir)
FFMPEGLibs=$(FFMPEGLibDir)/libavdevice.so\
	$(FFMPEGLibDir)/libavfilter.so\
	$(FFMPEGLibDir)/libswresample.so\
	$(FFMPEGLibDir)/libavcodec.so\
	$(FFMPEGLibDir)/libavformat.so\
	$(FFMPEGLibDir)/libavutil.so\
	$(FFMPEGLibDir)/libswscale.so
FFMPEGIncludes=$(FFMPEGDir)
FFMPEGDefines=-DUSE_FFMPEG -DLINUX

SOURCES=$(wildcard *.cpp)
OBJS=$(patsubst %.cpp, $(OBJDIR)/%.o, $(SOURCES))
TARGET=$(OUTDIR)/Microsoft.Psi.Media_Interop.so

OBJDIR=obj
OUTDIR=bin
DIRS=$(OBJDIR) $(OUTDIR)

$(TARGET): $(OBJS) | $(DIRS)
	g++ -g -shared -Wl,-Bsymbolic -o $@ $(OBJS) $(FFMPEGLibs)

$(OBJDIR)/%.o: %.cpp | $(DIRS)
	g++ -g -fPIC -pthread -std=c++11 -c -o $@ $< -I$(FFMPEGIncludes) $(FFMPEGDefines) -Wno-deprecated-declarations

$(DIRS):
	mkdir -p $@

clean:
	rm -f $(OBJS) $(TARGET)
