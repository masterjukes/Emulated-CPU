@const VGA_CTRL_BYTE 0xF0000000
@const VGA_TEXT_ADDR 0xF0180001
@const VGA_GRAPHICS_ADDR 0xF0000001
@const VGA_TEXT_NOBLINK 0b00000001
@const VGA_TEXT_BLINK 0b00000101
@const VGA_GRAPHICS_ENABLE 0x0

@const 

 
:STRCPY
LOAD %31 %0
while %31 != #0
    STORE %1 %31
    INC %1
    STORE %1 %2 
    INC %1
    INC %0
    LOAD %31 %0
end
RET


:STRCMP
LOAD %31 %0
LOAD %30 %1
while %31 == %30
    if %31 == 0x0
        SUBL %0 %31 %30
        RET
    end
    INC %0
    INC %1
    LOAD %31 %0
    LOAD %30 %1
end
SUBL %0 %31 %30
RET




