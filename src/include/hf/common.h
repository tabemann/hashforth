/* Copyright (c) 2018-2019, Travis Bemann
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE. */

#ifndef HF_TYPES_H
#define HF_TYPES_H

#include <stdint.h>

/* Forward declarations */

#ifdef CELL_16

typedef uint16_t hf_cell_t;
typedef int16_t hf_sign_cell_t;

#else
#ifdef CELL_32

typedef uint32_t hf_cell_t;
typedef int32_t hf_sign_cell_t;

#else /* CELL_64 */

typedef uint64_t hf_cell_t;
typedef int64_t hf_sign_cell_t;

#endif
#endif

typedef uint8_t hf_byte_t;

typedef int8_t hf_sign_byte_t;

#ifdef TOKEN_8_16

typedef uint8_t hf_token_t;
typedef uint16_t hf_full_token_t;

#else
#ifdef TOKEN_16

typedef uint16_t hf_token_t;
typedef uint16_t hf_full_token_t;

#else
#ifdef TOKEN_16_32

typedef uint16_t hf_token_t;
typedef uint32_t hf_full_token_t;

#else /* TOKEN_32 */

typedef uint32_t hf_token_t;
typedef uint32_t hf_full_token_t;

#endif
#endif
#endif

typedef hf_cell_t hf_flags_t;

typedef hf_sign_cell_t hf_sys_index_t;

struct hf_word_t;
typedef struct hf_word_t hf_word_t;

struct hf_sys_t;
typedef struct hf_sys_t hf_sys_t;

struct hf_global_t;
typedef struct hf_global_t hf_global_t;

typedef void (*hf_prim_t)(hf_global_t* global);

typedef void (*hf_sys_prim_t)(hf_global_t* global);

/* Constants */

#define HF_TRUE ((hf_cell_t)(-1))
#define HF_FALSE ((hf_cell_t)0)

#define HF_INIT_WORD_SPACE_COUNT (16)
#define HF_INIT_DATA_STACK_COUNT (1024)
#define HF_INIT_RETURN_STACK_COUNT (1024)
#define HF_INIT_USER_SPACE_SIZE (65536 * sizeof(hf_cell_t))

#define HF_WORD_NORMAL (0)
#define HF_WORD_IMMEDIATE (1)
#define HF_WORD_COMPILE_ONLY (2)
#define HF_WORD_HIDDEN (4)

#define HF_WORD_MIN_USER_SPACE_LEFT (1024 * sizeof(hf_cell_t))

#define HF_PRIM_END (0)
#define HF_PRIM_NOP (1)
#define HF_PRIM_EXIT (2)
#define HF_PRIM_BRANCH (3)
#define HF_PRIM_0BRANCH (4)
#define HF_PRIM_LIT (5)
#define HF_PRIM_DATA (6)
#define HF_PRIM_NEW_COLON (7)
#define HF_PRIM_NEW_CREATE (8)
#define HF_PRIM_SET_DOES (9)
#define HF_PRIM_FINISH (10)
#define HF_PRIM_EXECUTE (11)
#define HF_PRIM_DROP (12)
#define HF_PRIM_DUP (13)
#define HF_PRIM_SWAP (14)
#define HF_PRIM_ROT (15)
#define HF_PRIM_PICK (16)
#define HF_PRIM_ROLL (17)
#define HF_PRIM_LOAD (18)
#define HF_PRIM_STORE (19)
#define HF_PRIM_LOAD_8 (20)
#define HF_PRIM_STORE_8 (21)
#define HF_PRIM_EQ (22)
#define HF_PRIM_NE (23)
#define HF_PRIM_LT (24)
#define HF_PRIM_GT (25)
#define HF_PRIM_ULT (26)
#define HF_PRIM_UGT (27)
#define HF_PRIM_NOT (28)
#define HF_PRIM_AND (29)
#define HF_PRIM_OR (30)
#define HF_PRIM_XOR (31)
#define HF_PRIM_LSHIFT (32)
#define HF_PRIM_RSHIFT (33)
#define HF_PRIM_ARSHIFT (34)
#define HF_PRIM_ADD (35)
#define HF_PRIM_SUB (36)
#define HF_PRIM_MUL (37)
#define HF_PRIM_DIV (38)
#define HF_PRIM_MOD (39)
#define HF_PRIM_UDIV (40)
#define HF_PRIM_UMOD (41)
#define HF_PRIM_LOAD_R (42)
#define HF_PRIM_STORE_R (43)
#define HF_PRIM_POP_R (44)
#define HF_PRIM_LOAD_SP (45)
#define HF_PRIM_STORE_SP (46)
#define HF_PRIM_LOAD_RP (47)
#define HF_PRIM_STORE_RP (48)
#define HF_PRIM_TO_BODY (49)
#define HF_PRIM_WORD_TO_NAME (50)
#define HF_PRIM_NAME_TO_WORD (51)
#define HF_PRIM_WORD_TO_NEXT (52)
#define HF_PRIM_NEXT_TO_WORD (53)
#define HF_PRIM_WORD_TO_FLAGS (54)
#define HF_PRIM_FLAGS_TO_WORD (55)
#define HF_PRIM_CONFIG (56)
#define HF_PRIM_LOAD_16 (57)
#define HF_PRIM_STORE_16 (58)
#define HF_PRIM_LOAD_32 (59)
#define HF_PRIM_STORE_32 (60)
#define HF_PRIM_SET_WORD_COUNT (61)
#define HF_PRIM_SYS (62)
#define HF_PRIM_SYS_LOOKUP (63)

#define HF_SYS_UNDEFINED (0)
#define HF_SYS_TYPE (1)
#define HF_SYS_KEY (2)
#define HF_SYS_ACCEPT (3)
#define HF_SYS_BYE (4)
#define HF_SYS_ALLOCATE (5)
#define HF_SYS_RESIZE (6)
#define HF_SYS_FREE (7)
#define HF_SYS_OPEN (8)
#define HF_SYS_CLOSE (9)
#define HF_SYS_READ (10)
#define HF_SYS_WRITE (11)
#define HF_SYS_GET_NONBLOCKING (12)
#define HF_SYS_SET_NONBLOCKING (13)
#define HF_SYS_ISATTY (14)
#define HF_SYS_POLL (15)
#define HF_SYS_GET_MONOTONIC_TIME (16)
#define HF_SYS_GET_TRACE (17)
#define HF_SYS_SET_TRACE (18)
#define HF_SYS_GET_SBASE (19)
#define HF_SYS_SET_SBASE (20)

#define HF_MAX_STD_SERVICES (20)
#define HF_MAX_NSTD_SERVICES (0)

#define HF_OPEN_RDONLY (1)
#define HF_OPEN_WRONLY (2)
#define HF_OPEN_RDWR (4)
#define HF_OPEN_APPEND (8)
#define HF_OPEN_CREAT (16)
#define HF_OPEN_EXCL (32)
#define HF_OPEN_TRUNC (64)

#define HF_POLL_IN (1)
#define HF_POLL_OUT (2)
#define HF_POLL_PRI (4)
#define HF_POLL_ERR (8)
#define HF_POLL_HUP (16)
#define HF_POLL_NVAL (32)

#define HF_WOULDBLOCK (1);

/* Definitions */

struct hf_word_t {
  hf_prim_t primitive;
  hf_flags_t flags;
  hf_cell_t name_length;
  hf_byte_t* name;
  void* data;
  hf_token_t* secondary;
  hf_full_token_t next;
};

struct hf_sys_t {
  hf_cell_t defined;
  hf_sys_prim_t primitive;
  hf_cell_t name_length;
  hf_byte_t* name;
};

struct hf_global_t {
  hf_word_t* words;
  hf_cell_t word_count;
  hf_cell_t word_space_count;
  hf_sys_t* std_services;
  hf_cell_t std_service_count;
  hf_cell_t std_service_space_count;
  hf_sys_t* nstd_services;
  hf_cell_t nstd_service_count;
  hf_cell_t nstd_service_space_count;
  hf_word_t* current_word;
  hf_token_t* ip;
  hf_cell_t* data_stack;
#ifdef STACK_TRACE
  hf_cell_t* data_stack_base;
#endif
  hf_token_t** return_stack;
#ifdef TRACE
  hf_cell_t level;
#endif
  hf_cell_t trace;
};

#endif /* HF_TYPES_H */
