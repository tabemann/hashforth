\ Copyright (c) 2019, Travis Bemann
\ All rights reserved.
\ 
\ Redistribution and use in source and binary forms, with or without
\ modification, are permitted provided that the following conditions are met:
\ 
\ 1. Redistributions of source code must retain the above copyright notice,
\    this list of conditions and the following disclaimer.
\ 
\ 2. Redistributions in binary form must reproduce the above copyright notice,
\    this list of conditions and the following disclaimer in the documentation
\    and/or other materials provided with the distribution.
\ 
\ 3. Neither the name of the copyright holder nor the names of its
\    contributors may be used to endorse or promote products derived from
\    this software without specific prior written permission.
\ 
\ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
\ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
\ IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
\ ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
\ LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
\ CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
\ SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
\ INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
\ CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
\ ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
\ POSSIBILITY OF SUCH DAMAGE.

FORTH-WORDLIST TASK-WORDLIST 2 SET-ORDER

100 2 NEW-BCHAN CONSTANT MY-BCHAN

: SENDER
  PAUSE-COUNT 0 BEGIN
    DUP MY-BCHAN SEND-BCHAN 1 +
    DUP $1000 MOD 0 = IF
      PAUSE-COUNT ROT OVER SWAP - $1000 SWAP / ." <" . DUP (.) ." > " SWAP
    THEN
  AGAIN ;

: RECEIVER
  PAUSE-COUNT 0 BEGIN
    MY-BCHAN RECV-BCHAN DROP 1 +
    DUP $1000 MOD 0 = IF
      PAUSE-COUNT ROT OVER SWAP - $1000 SWAP / ." [" . DUP (.) ." ] " SWAP
    THEN
  AGAIN ;

256 256 256 ' SENDER NEW-TASK CONSTANT SENDER-TASK
256 256 256 ' RECEIVER NEW-TASK CONSTANT RECEIVER-TASK

SENDER-TASK ACTIVATE-TASK RECEIVER-TASK ACTIVATE-TASK

SLEEP-TASK DEACTIVATE-TASK MAIN-TASK DEACTIVATE-TASK

\ KEY DROP SENDER-TASK DEACTIVATE-TASK RECEIVER-TASK DEACTIVATE-TASK
