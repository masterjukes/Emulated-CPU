


 
:PUTSTR
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


:ITOA
DEC %1
MOVL %31 0x0
STORE %1 %31
MOVL %29 0xA
MOVL %28 '0'
while %0 > #0 
    MODL %30 %0 %29 
    DEC %1
    ADDL %27 %28 %30
    STORE %1 %27
    DIVL %0 %0 %29
end
PUSH %1
POP %0
RET




:HTOA
MOVL %31 0x8
MOVL %28 0x0F
MOVL %27 #4
MOVL %26 .HTOA_HEXVALUES
while %31 != #0
    DEC %31 
    ADDL %30 %1 %31
    ANDL %29 %0 %28
    ADDL %29 %29 %26
    LOAD %25 %29
    STORE %30 %25
    SHRL %0 %0 %27   
end
MOVL %29 0x0
MOVL %31 0x8
ADDL %30 %1 %31
STORE %30 %29 
RET
    


:HTOA_HEXVALUES
DATA '0'
DATA '1'
DATA '2'
DATA '3'
DATA '4'
DATA '5'
DATA '6'
DATA '7'
DATA '8'
DATA '9'
DATA 'A'
DATA 'B'
DATA 'C'
DATA 'D'
DATA 'E'
DATA 'F'





