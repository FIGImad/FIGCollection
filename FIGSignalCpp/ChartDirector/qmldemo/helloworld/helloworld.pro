QT += quick
CONFIG += c++11

SOURCES += main.cpp \
	qmlchartviewer.cpp \
    helloworld.cpp

HEADERS += qmlchartviewer.h \
    helloworld.h

RESOURCES += helloworld.qrc
	
INCLUDEPATH += ../../include

DEFINES += CHARTDIR_HIDE_OBSOLETE _CRT_SECURE_NO_WARNINGS

CONFIG += warn_off

#
# On Windows, need to link with chartdir70.lib and also copy the chartdir70.dll
# to the same directory as the compiled executable.
#
win32 {
    contains(QMAKE_HOST.arch, x86_64) {
		LIBS += $$PWD/../../lib64/chartdir70.lib
		QMAKE_POST_LINK = copy /Y $$shell_quote($$shell_path($$PWD/../../lib64/chartdir70.dll)) $(DESTDIR)
    } else {
		LIBS += $$PWD/../../lib32/chartdir70.lib
		QMAKE_POST_LINK = copy /Y $$shell_quote($$shell_path($$PWD/../../lib32/chartdir70.dll)) $(DESTDIR)
    }
}

#
# On macOS, need to link with libchartdir.7.dylib. The libchartdir.7.dylib need 
# to be copied to the Contents/Frameworks subdirectory of the App bundle.
#
macx:LIBS += -L$$PWD/../../lib -lchartdir
macx:ChartDirector.files = $$PWD/../../lib/libchartdir.7.dylib
macx:ChartDirector.path = Contents/Frameworks
macx:QMAKE_BUNDLE_DATA = ChartDirector

#
# On Linux, the link path and runtime path is set up assuming the libchartdir.so
# is in ../../lib.
#
unix:!macx:LIBS += -L$$PWD/../../lib -lchartdir
unix:!macx:QMAKE_RPATHDIR += $$PWD/../../lib