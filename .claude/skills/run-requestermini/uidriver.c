// uidriver — synthesize mouse/keyboard events on macOS via CoreGraphics.
//
// Avalonia's accessibility peers do NOT respond to AppleScript AXPress
// (clicking a "radio button" tab via System Events does nothing), so we
// drive the GUI with real HID events posted at screen-point coordinates.
//
// Build:  clang uidriver.c -o uidriver -framework ApplicationServices
// Usage:
//   uidriver click X Y     mouse click at screen point (X,Y)
//   uidriver type "STR"    type a UTF-8 string (layout-independent)
//   uidriver selectall     Cmd+A
//   uidriver delete        Backspace
//
// Coordinates are in *screen points* — the same units AppleScript's
// `position`/`size` report — so feed accessibility positions straight in,
// no Retina/pixel scaling needed.
#include <ApplicationServices/ApplicationServices.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>

static void click(double x, double y) {
    CGPoint p = CGPointMake(x, y);
    CGEventRef mv = CGEventCreateMouseEvent(NULL, kCGEventMouseMoved, p, kCGMouseButtonLeft);
    CGEventPost(kCGHIDEventTap, mv); CFRelease(mv); usleep(50000);
    CGEventRef d = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseDown, p, kCGMouseButtonLeft);
    CGEventPost(kCGHIDEventTap, d); CFRelease(d); usleep(40000);
    CGEventRef u = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseUp, p, kCGMouseButtonLeft);
    CGEventPost(kCGHIDEventTap, u); CFRelease(u);
}

static void typeUTF8(const char *s) {
    CFStringRef str = CFStringCreateWithCString(NULL, s, kCFStringEncodingUTF8);
    CFIndex n = CFStringGetLength(str);
    for (CFIndex i = 0; i < n; i++) {
        UniChar c = CFStringGetCharacterAtIndex(str, i);
        CGEventRef d = CGEventCreateKeyboardEvent(NULL, 0, true);
        CGEventKeyboardSetUnicodeString(d, 1, &c);
        CGEventPost(kCGHIDEventTap, d); CFRelease(d); usleep(6000);
        CGEventRef u = CGEventCreateKeyboardEvent(NULL, 0, false);
        CGEventKeyboardSetUnicodeString(u, 1, &c);
        CGEventPost(kCGHIDEventTap, u); CFRelease(u); usleep(6000);
    }
    CFRelease(str);
}

static void key(CGKeyCode code, CGEventFlags flags) {
    CGEventRef d = CGEventCreateKeyboardEvent(NULL, code, true);
    CGEventSetFlags(d, flags);
    CGEventPost(kCGHIDEventTap, d); CFRelease(d); usleep(20000);
    CGEventRef u = CGEventCreateKeyboardEvent(NULL, code, false);
    CGEventSetFlags(u, flags);
    CGEventPost(kCGHIDEventTap, u); CFRelease(u); usleep(20000);
}

int main(int argc, char **argv) {
    if (argc < 2) return 1;
    if (strcmp(argv[1], "click") == 0 && argc >= 4) {
        click(atof(argv[2]), atof(argv[3]));
    } else if (strcmp(argv[1], "type") == 0 && argc >= 3) {
        typeUTF8(argv[2]);
    } else if (strcmp(argv[1], "selectall") == 0) {
        key(0, kCGEventFlagMaskCommand); // 'a' = keycode 0
    } else if (strcmp(argv[1], "delete") == 0) {
        key(51, 0); // delete/backspace
    } else {
        fprintf(stderr, "usage: uidriver {click X Y|type STR|selectall|delete}\n");
        return 1;
    }
    return 0;
}
