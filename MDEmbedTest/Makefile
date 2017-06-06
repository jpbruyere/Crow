export PKG_CONFIG_PATH = /opt/mono/lib/pkgconfig

CC = gcc
CFLAGS = -Wall -Wextra -O2 -g -DDEBUG
LDFLAGS = -L/devel/crow/build/Debug 

TARGET  = mdembed
SOURCES = main.c
OBJECTS = $(SOURCES:.c=.o)

CFLAGS += -lcrow
CFLAGS += `pkg-config --cflags --libs mono-2` 
CFLAGS += `pkg-config --cflags --libs cairo` 
CFLAGS += `pkg-config --cflags --libs gl`

.PHONY: all
all: ${TARGET} test.exe	test2.exe

$(TARGET): $(OBJECTS)
	$(CC) $(CFLAGS)  ${LDFLAGS} -o $(TARGET) $(OBJECTS)
test.exe : test.cs
	mcs -unsafe test.cs
test2.exe : test2.cs
	mcs -unsafe test2.cs

.PHONY: clean
clean:
	-rm -f ${TARGET} ${OBJECTS} test.exe test2.exe

.PHONY: run
run:
	-./${TARGET} ../build/Debug/testDrm.exe
