%asciiof %in #out_addr
PUSH %0
PUSH %3
PUSH %in
PUSH %out_addr
POP %3
POP %0
MOVL %15 .ASCIIOF
CALL %15
POP %3
POP %0

:ASCIIOF
PUSH %1
PUSH %2
*andli %1 %0 0xF0000000
*shrli %2 %1 #28
*andli %2 %2 0x0f
*cmpi %2 #10
*jlti .bBELOW10
*addi %2 %2 0x37
*jmpi .bSECONDNIB
:bBELOW10
*addi %2 %2 0x30
:bSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x0F000000
*shrli %2 %1 #24
*cmpi %2 #10
*jlti .qBELOW10
*addi %2 %2 0x37
*jmpi .qSECONDNIB
:qBELOW10
*addi %2 %2 0x30
:qSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x00F00000
*shrli %2 %1 #20
*cmpi %2 #10
*jlti .cBELOW10
*addi %2 %2 0x37
*jmpi .cSECONDNIB
:cBELOW10
*addi %2 %2 0x30
:cSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x000F0000
*shrli %2 %1 #16
*cmpi %2 #10
*jlti .dBELOW10
*addi %2 %2 0x37
*jmpi .dSECONDNIB
:dBELOW10
*addi %2 %2 0x30
:dSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x0000F000
*shrli %2 %1 #12
*cmpi %2 #10
*jlti .eBELOW10
*addi %2 %2 0x37
*jmpi .eSECONDNIB
:eBELOW10
*addi %2 %2 0x30
:eSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x00000F00
*shrli %2 %1 #8
*cmpi %2 #10
*jlti .fBELOW10
*addi %2 %2 0x37
*jmpi .fSECONDNIB
:fBELOW10
*addi %2 %2 0x30
:fSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x000000F0
*shrli %2 %1 #4
*cmpi %2 #10
*jlti .gBELOW10
*addi %2 %2 0x37
*jmpi .gSECONDNIB
:gBELOW10
*addi %2 %2 0x30
:gSECONDNIB
STORE %3 %2
INC %3


*andli %1 %0 0x0000000F
*shrli %2 %1 #0
*cmpi %2 #10
*jlti .hBELOW10
*addi %2 %2 0x37
*jmpi .hSECONDNIB
:hBELOW10
*addi %2 %2 0x30
:hSECONDNIB
STORE %3 %2
INC %3
*store_b %3 0x00

POP %2
POP %1
RET
