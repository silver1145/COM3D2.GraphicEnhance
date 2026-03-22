.code
lbs_skinning_sse2 PROC
    mov     r11, rsp
    sub     rsp, 0A8h

    movaps  [rsp + 0A8h - 088h], xmm13
    movaps  [rsp + 0A8h - 098h], xmm14
    movaps  [rsp + 0A8h - 0A8h], xmm15

    prefetcht0 byte ptr [rcx]

    mov     rax, [rsp + 0A8h + 028h]
    movups  xmm13, xmmword ptr [rcx]
    movups  xmm14, xmmword ptr [rcx + 010h]
    movups  xmm15, xmmword ptr [rcx + 020h]

    test    rax, rax
    jz      loc_end

    movaps  xmmword ptr [r11 - 018h], xmm6
    movaps  xmmword ptr [r11 - 028h], xmm7
    movaps  xmmword ptr [r11 - 038h], xmm8

    sub     r8, rdx
    lea     r10, [rdx + 020h]
    sub     r9, rdx

    movaps  xmmword ptr [r11 - 048h], xmm9
    movaps  xmmword ptr [r11 - 058h], xmm10
    movaps  xmmword ptr [r11 - 068h], xmm11
    movaps  xmmword ptr [r11 - 078h], xmm12

    loc_loop_start:

    prefetcht0 byte ptr [r10 - 020h]
    prefetcht0 byte ptr [r8 + r10 - 020h]

    movups  xmm2,  xmmword ptr [r8 + r10 - 020h]
    movups  xmm3,  xmmword ptr [r8 + r10 - 010h]
    movups  xmm4,  xmmword ptr [r8 + r10]

    movaps  xmm10, xmm2
    movaps  xmm0,  xmm2
    movaps  xmm1,  xmm2
    shufps  xmm0,  xmm2, 000h
    shufps  xmm1,  xmm2, 0AAh
    shufps  xmm10, xmm2, 055h

    movaps  xmm12, xmm4
    movaps  xmm11, xmm3

    movups  xmm7,  xmmword ptr [r8 + r10 + 010h]
    movups  xmm5,  xmmword ptr [r10 - 010h]
    movups  xmm6,  xmmword ptr [r10 - 020h]

    shufps  xmm12, xmm4, 055h
    shufps  xmm11, xmm3, 055h
    shufps  xmm2,  xmm2, 0FFh

    mulps   xmm0,  xmm6
    mulps   xmm10, xmm5
    mulps   xmm11, xmm5

    movups  xmm8,  xmmword ptr [r10]
    movups  xmm9,  xmmword ptr [r10 + 010h]

    addps   xmm10, xmm0

    movaps  xmm0, xmm3
    mulps   xmm1, xmm8
    shufps  xmm0, xmm3, 000h
    mulps   xmm2, xmm9

    addps   xmm10, xmm1

    movaps  xmm1, xmm3
    shufps  xmm1, xmm3, 0AAh

    addps   xmm10, xmm2

    mulps   xmm0, xmm6
    addps   xmm11, xmm0

    movaps  xmm0, xmm4
    movaps  xmm2, xmm10
    shufps  xmm0, xmm4, 000h

    mulps   xmm1, xmm8
    shufps  xmm2, xmm10, 055h

    addps   xmm11, xmm1

    movaps  xmm1, xmm4
    shufps  xmm1, xmm4, 0AAh

    mulps   xmm0, xmm6
    shufps  xmm4, xmm4, 0FFh

    mulps   xmm1, xmm8
    mulps   xmm4, xmm9
    mulps   xmm12, xmm5

    addps   xmm12, xmm0

    movaps  xmm0, xmm7
    addps   xmm12, xmm1

    movaps  xmm1, xmm7
    addps   xmm12, xmm4

    shufps  xmm0, xmm7, 000h
    shufps  xmm1, xmm7, 0AAh
    movaps  xmm4, xmm7

    shufps  xmm4, xmm7, 055h

    mulps   xmm0, xmm6
    mulps   xmm1, xmm8
    mulps   xmm4, xmm5

    mulps   xmm2, xmm14
    shufps  xmm3, xmm3, 0FFh

    movups  xmm5, xmmword ptr [rcx + 030h]

    addps   xmm4, xmm0

    movaps  xmm0, xmm10
    addps   xmm4, xmm1

    shufps  xmm0, xmm10, 000h

    mulps   xmm3, xmm9

    shufps  xmm7, xmm7, 0FFh

    movaps  xmm1, xmm10

    addps   xmm11, xmm3

    shufps  xmm1, xmm10, 0AAh

    mulps   xmm0, xmm13

    shufps  xmm10, xmm10, 0FFh

    movaps  xmm3, xmm11

    addps   xmm2, xmm0

    mulps   xmm1, xmm15

    mulps   xmm10, xmm5

    mulps   xmm7, xmm9

    addps   xmm2, xmm1

    addps   xmm4, xmm7

    addps   xmm2, xmm10

    movups  xmmword ptr [r9 + r10 - 020h], xmm2

    shufps  xmm3, xmm11, 055h

    movaps  xmm0, xmm11
    movaps  xmm1, xmm11

    shufps  xmm0, xmm11, 000h
    mulps   xmm3, xmm14

    shufps  xmm1, xmm11, 0AAh

    movaps  xmm2, xmm12

    add     r10, 040h
    dec     rax

    mulps   xmm0, xmm13

    mulps   xmm1, xmm15

    shufps  xmm2, xmm12, 055h

    addps   xmm3, xmm0

    movaps  xmm0, xmm12

    addps   xmm3, xmm1

    movaps  xmm1, xmm12

    shufps  xmm0, xmm12, 000h

    shufps  xmm1, xmm12, 0AAh

    mulps   xmm2, xmm14

    mulps   xmm0, xmm13

    mulps   xmm1, xmm15

    shufps  xmm11, xmm11, 0FFh

    addps   xmm2, xmm0

    movaps  xmm0, xmm4

    addps   xmm2, xmm1

    movaps  xmm1, xmm4

    shufps  xmm0, xmm4, 000h

    shufps  xmm1, xmm4, 0AAh

    mulps   xmm11, xmm5

    addps   xmm3, xmm11

    movups  xmmword ptr [r9 + r10 - 050h], xmm3

    movaps  xmm3, xmm4

    shufps  xmm3, xmm4, 055h

    mulps   xmm0, xmm13

    mulps   xmm3, xmm14

    mulps   xmm1, xmm15

    shufps  xmm12, xmm12, 0FFh

    addps   xmm3, xmm0

    addps   xmm3, xmm1

    shufps  xmm4, xmm4, 0FFh

    mulps   xmm12, xmm5

    addps   xmm2, xmm12

    mulps   xmm4, xmm5

    movups  xmmword ptr [r9 + r10 - 040h], xmm2

    addps   xmm3, xmm4

    movups  xmmword ptr [r9 + r10 - 030h], xmm3

    jnz     loc_loop_start

    movaps  xmm12, [rsp + 0A8h - 078h]
    movaps  xmm11, [rsp + 0A8h - 068h]
    movaps  xmm10, [rsp + 0A8h - 058h]
    movaps  xmm9,  [rsp + 0A8h - 048h]
    movaps  xmm8,  [rsp + 0A8h - 038h]
    movaps  xmm7,  [rsp + 0A8h - 028h]
    movaps  xmm6,  [rsp + 0A8h - 018h]

    loc_end:

    movaps  xmm13, [rsp + 0A8h - 088h]
    movaps  xmm14, [rsp + 0A8h - 098h]
    movaps  xmm15, [rsp + 0A8h - 0A8h]

    add     rsp, 0A8h
    ret

lbs_skinning_sse2 ENDP
END
