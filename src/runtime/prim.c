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

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <strings.h>
#include <stdint.h>
#include "hf/common.h"
#include "hf/inner.h"

/* Forward declarations */

/* Register a primitive */
void hf_register_prim(hf_global_t* global, hf_full_token_t token,
		      hf_prim_t primitive, void** user_space_current);

/* End primitive */
void hf_prim_end(hf_global_t* global);

/* NOP primitive */
void hf_prim_nop(hf_global_t* global);

/* EXIT primitive */
void hf_prim_exit(hf_global_t* global);

/* BRANCH primitive */
void hf_prim_branch(hf_global_t* global);

/* 0BRANCH primitive */
void hf_prim_0branch(hf_global_t* global);

/* (LIT) primitive */
void hf_prim_lit(hf_global_t* global);

/* (LITC) primitve */
void hf_prim_lit_8(hf_global_t* global);

/* (LITH) primitive */
void hf_prim_lit_16(hf_global_t* global);

/* (LITW) primitive */
void hf_prim_lit_32(hf_global_t* global);

/* (DATA) primitive */
void hf_prim_data(hf_global_t* global);

/* NEW-COLON primitive */
void hf_prim_new_colon(hf_global_t* global);

/* NEW-CREATE primitive */
void hf_prim_new_create(hf_global_t* global);

/* SET-DOES> primitive */
void hf_prim_set_does(hf_global_t* global);

/* FINISH primitive */
void hf_prim_finish(hf_global_t* global);

/* EXECUTE primitive */
void hf_prim_execute(hf_global_t* global);

/* DROP primitive */
void hf_prim_drop(hf_global_t* global);

/* DUP primitive */
void hf_prim_dup(hf_global_t* global);

/* SWAP primitive */
void hf_prim_swap(hf_global_t* global);

/* OVER primitive */
void hf_prim_over(hf_global_t* global);

/* ROT primitive */
void hf_prim_rot(hf_global_t* global);

/* PICK primitive */
void hf_prim_pick(hf_global_t* global);

/* ROLL primitive */
void hf_prim_roll(hf_global_t* global);

/* @ primitive */
void hf_prim_load(hf_global_t* global);

/* ! primitive */
void hf_prim_store(hf_global_t* global);

/* C@ primitive */
void hf_prim_load_8(hf_global_t* global);

/* C! primitive */
void hf_prim_store_8(hf_global_t* global);

/* = primitive */
void hf_prim_eq(hf_global_t* global);

/* <> primitive */
void hf_prim_ne(hf_global_t* global);

/* < primitive */
void hf_prim_lt(hf_global_t* global);

/* > primitive */
void hf_prim_gt(hf_global_t* global);

/* U< primitive */
void hf_prim_ult(hf_global_t* global);

/* U> primitive */
void hf_prim_ugt(hf_global_t* global);

/* NOT primitive */
void hf_prim_not(hf_global_t* global);

/* AND primitive */
void hf_prim_and(hf_global_t* global);

/* OR primitive */
void hf_prim_or(hf_global_t* global);

/* XOR primitive */
void hf_prim_xor(hf_global_t* global);

/* LSHIFT primitive */
void hf_prim_lshift(hf_global_t* global);

/* RSHIFT primitive */
void hf_prim_rshift(hf_global_t* global);

/* ARSHIFT primitive */
void hf_prim_arshift(hf_global_t* global);

/* + primitive */
void hf_prim_add(hf_global_t* global);

/* - primitive */
void hf_prim_sub(hf_global_t* global);

/* * primitive */
void hf_prim_mul(hf_global_t* global);

/* / primitive */
void hf_prim_div(hf_global_t* global);

/* MOD primitive */
void hf_prim_mod(hf_global_t* global);

/* U/ primitive */
void hf_prim_udiv(hf_global_t* global);

/* UMOD primitive */
void hf_prim_umod(hf_global_t* global);

/* MUX primitive */
void hf_prim_mux(hf_global_t* global);

/* /MUX primitive */
void hf_prim_rmux(hf_global_t* global);

/* R@ primitive */
void hf_prim_load_r(hf_global_t* global);

/* >R primitive */
void hf_prim_push_r(hf_global_t* global);

/* R> primitive */
void hf_prim_pop_r(hf_global_t* global);

/* SP@ primitive */
void hf_prim_load_sp(hf_global_t* global);

/* SP! primitive */
void hf_prim_store_sp(hf_global_t* global);

/* RP@ primitive */
void hf_prim_load_rp(hf_global_t* global);

/* RP! primitive */
void hf_prim_store_rp(hf_global_t* global);

/* >BODY primitive */
void hf_prim_to_body(hf_global_t* global);

/* H@ primitive */
void hf_prim_load_16(hf_global_t* global);

/* H! primitive */
void hf_prim_store_16(hf_global_t* global);

/* W@ primitive */
void hf_prim_load_32(hf_global_t* global);

/* W! primitive */
void hf_prim_store_32(hf_global_t* global);

/* SET-WORD-COUNT */
void hf_prim_set_word_count(hf_global_t* global);

/* SYS primitive */
void hf_prim_sys(hf_global_t* global);

/* SYS-LOOKUP primitive */
void hf_prim_sys_lookup(hf_global_t* global);

/* D+ primitive */
void hf_prim_d_add(hf_global_t* global);

/* D- primitive */
void hf_prim_d_sub(hf_global_t* global);

/* D* primitive */
void hf_prim_d_mul(hf_global_t* global);

/* D/ primitive */
void hf_prim_d_div(hf_global_t* global);

/* DMOD primitive */
void hf_prim_d_mod(hf_global_t* global);

/* DU/ primitive */
void hf_prim_d_udiv(hf_global_t* global);

/* DUMOD primitive */
void hf_prim_d_umod(hf_global_t* global);

/* DNOT primitive */
void hf_prim_d_not(hf_global_t* global);

/* DAND primitive */
void hf_prim_d_and(hf_global_t* global);

/* DOR primitive */
void hf_prim_d_or(hf_global_t* global);

/* DXOR primitive */
void hf_prim_d_xor(hf_global_t* global);

/* DLSHIFT primitive */
void hf_prim_d_lshift(hf_global_t* global);

/* DRSHIFT primitive */
void hf_prim_d_rshift(hf_global_t* global);

/* DARSHIFT primitive */
void hf_prim_d_arshift(hf_global_t* global);

/* D< primitive */
void hf_prim_d_lt(hf_global_t* global);

/* D> primitive */
void hf_prim_d_gt(hf_global_t* global);

/* D= primitive */
void hf_prim_d_eq(hf_global_t* global);

/* D<> primitive */
void hf_prim_d_ne(hf_global_t* global);

/* DU< primitive */
void hf_prim_d_ult(hf_global_t* global);

/* DU> primitive */
void hf_prim_d_ugt(hf_global_t* global);

/* * / primitive */
void hf_prim_mul_div(hf_global_t* global);

/* *RSHIFT primitive */
void hf_prim_mul_rshift(hf_global_t* global);

/* LSHIFT/ primitive */
void hf_prim_lshift_div(hf_global_t* global);

/* U* / primitive */
void hf_prim_umul_div(hf_global_t* global);

/* U*RSHIFT primitive */
void hf_prim_umul_rshift(hf_global_t* global);

/* ULSHIFT/ primitive */
void hf_prim_ulshift_div(hf_global_t* global);

/* * /MOD primitive */
void hf_prim_mul_div_mod(hf_global_t* global);

/* U* /MOD primitive */
void hf_prim_umul_div_mod(hf_global_t* global);

/* Macros */

/* Convert a C boolean into a Forth boolean */
#define HF_BOOL(value) ((value) ? HF_TRUE : HF_FALSE)

/* Definitions */

/* Register a primitive */
void hf_register_prim(hf_global_t* global, hf_full_token_t token,
		      hf_prim_t primitive, void** user_space_current) {
  hf_word_t* word;
  word = hf_new_word(global, token);
  word->data = NULL;
  word->primitive = primitive;
  word->secondary = NULL;
}

/* Register primitives */
void hf_register_prims(hf_global_t* global, void** user_space_current) {
  hf_register_prim(global, HF_PRIM_END, hf_prim_end,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_NOP, hf_prim_nop,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_EXIT, hf_prim_exit,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_BRANCH, hf_prim_branch,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_0BRANCH, hf_prim_0branch,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LIT, hf_prim_lit,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LIT_8, hf_prim_lit_8,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LIT_16, hf_prim_lit_16,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LIT_32, hf_prim_lit_32,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_DATA, hf_prim_data,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_NEW_COLON, hf_prim_new_colon,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_NEW_CREATE, hf_prim_new_create,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_SET_DOES, hf_prim_set_does,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_FINISH, hf_prim_finish,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_EXECUTE, hf_prim_execute,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_DROP, hf_prim_drop,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_DUP, hf_prim_dup,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_SWAP, hf_prim_swap,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_OVER, hf_prim_over,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_ROT, hf_prim_rot,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_PICK, hf_prim_pick,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_ROLL, hf_prim_roll,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD, hf_prim_load,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_STORE, hf_prim_store,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD_8, hf_prim_load_8,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_STORE_8, hf_prim_store_8,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_EQ, hf_prim_eq,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_NE, hf_prim_ne,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LT, hf_prim_lt,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_GT, hf_prim_gt,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_ULT, hf_prim_ult,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_UGT, hf_prim_ugt,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_NOT, hf_prim_not,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_AND, hf_prim_and,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_OR, hf_prim_or,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_XOR, hf_prim_xor,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LSHIFT, hf_prim_lshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_RSHIFT, hf_prim_rshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_ARSHIFT, hf_prim_arshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_ADD, hf_prim_add,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_SUB, hf_prim_sub,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_MUL, hf_prim_mul,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_DIV, hf_prim_div,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_MOD, hf_prim_mod,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_UDIV, hf_prim_udiv,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_UMOD, hf_prim_umod,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_MUX, hf_prim_mux,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_RMUX, hf_prim_rmux,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD_R, hf_prim_load_r,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_PUSH_R, hf_prim_push_r,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_POP_R, hf_prim_pop_r,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD_SP, hf_prim_load_sp,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_STORE_SP, hf_prim_store_sp,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD_RP, hf_prim_load_rp,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_STORE_RP, hf_prim_store_rp,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_TO_BODY, hf_prim_to_body,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD_16, hf_prim_load_16,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_STORE_16, hf_prim_store_16,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LOAD_32, hf_prim_load_32,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_STORE_32, hf_prim_store_32,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_SET_WORD_COUNT,
		   hf_prim_set_word_count, user_space_current);
  hf_register_prim(global, HF_PRIM_SYS, hf_prim_sys, user_space_current);
  hf_register_prim(global, HF_PRIM_D_ADD, hf_prim_d_add, user_space_current);
  hf_register_prim(global, HF_PRIM_D_SUB, hf_prim_d_sub,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_MUL, hf_prim_d_mul,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_DIV, hf_prim_d_div,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_MOD, hf_prim_d_mod,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_UDIV, hf_prim_d_udiv,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_UMOD, hf_prim_d_umod,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_NOT, hf_prim_d_not,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_AND, hf_prim_d_and,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_OR, hf_prim_d_or,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_XOR, hf_prim_d_xor,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_LSHIFT, hf_prim_d_lshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_RSHIFT, hf_prim_d_rshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_ARSHIFT, hf_prim_d_arshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_LT, hf_prim_d_lt,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_GT, hf_prim_d_gt,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_EQ, hf_prim_d_eq,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_NE, hf_prim_d_ne,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_ULT, hf_prim_d_ult,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_D_UGT, hf_prim_d_ugt,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_MUL_DIV, hf_prim_mul_div,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_MUL_RSHIFT, hf_prim_mul_rshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_LSHIFT_DIV, hf_prim_lshift_div,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_UMUL_DIV, hf_prim_umul_div,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_UMUL_RSHIFT, hf_prim_umul_rshift,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_ULSHIFT_DIV, hf_prim_ulshift_div,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_MUL_DIV_MOD, hf_prim_mul_div_mod,
		   user_space_current);
  hf_register_prim(global, HF_PRIM_UMUL_DIV_MOD, hf_prim_umul_div_mod,
		   user_space_current);
}

/* Enter primitive */
void hf_prim_enter(hf_global_t* global) {
  *(--global->return_stack) = global->ip;
  global->ip = global->current_word->secondary;

}

/* Do CREATE primitive */
void hf_prim_do_create(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->current_word->data;
}

/* Do DOES> primitive */
void hf_prim_do_does(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->current_word->data;
  *(--global->return_stack) = global->ip;
  global->ip = global->current_word->secondary;
}

/* End primitive */
void hf_prim_end(hf_global_t* global) {
  hf_set_int(global, HF_INT_TOKEN, HF_TRUE);
}

/* NOP primitive */
void hf_prim_nop(hf_global_t* global) {
}

/* EXIT primitive */
void hf_prim_exit(hf_global_t* global) {
  global->ip = HF_ALIGNED_TO_TOKEN(*global->return_stack++);
}

/* BRANCH primitive */
void hf_prim_branch(hf_global_t* global) {
#ifdef CELL_16
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#elif CELL_32
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#elif TOKEN_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#else
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif  
  global->ip = HF_ALIGNED_TO_TOKEN(*(hf_token_t**)global->ip);
}

/* 0BRANCH primitive */
void hf_prim_0branch(hf_global_t* global) {
#ifdef CELL_16
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#elif CELL_32
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#elif TOKEN_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#else
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif  
  if(!(*global->data_stack++)) {
    global->ip = HF_ALIGNED_TO_TOKEN(*(hf_token_t**)global->ip);
  } else {
    *(void**)(&global->ip) += sizeof(hf_token_t*);
  }
}

/* (LIT) primitive */
void hf_prim_lit(hf_global_t* global) {
#ifdef CELL_16
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#elif CELL_32
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#elif TOKEN_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#else
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif  
  *(--global->data_stack) = *(hf_cell_t*)global->ip;
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(hf_cell_t));
}

/* (LITC) primitve */
void hf_prim_lit_8(hf_global_t* global) {
  hf_cell_t value = *(uint8_t*)global->ip;
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(uint8_t));
#else
  global->ip =
    (hf_token_t*)HF_ALIGNED_TO_TOKEN((void*)global->ip + sizeof(uint8_t));
#endif
 *(--global->data_stack) =
   value < 128 ? value : value | (((hf_sign_cell_t)-1) & ~0xFF);
}

/* (LITH) primitive */
void hf_prim_lit_16(hf_global_t* global) {
  hf_cell_t value;
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(uint16_t));
#endif
  value = *(uint16_t*)global->ip;
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(uint16_t));
#elif TOKEN_16
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(uint16_t));
#else
  global->ip =
    (hf_token_t*)HF_ALIGNED_TO_TOKEN((void*)global->ip + sizeof(uint16_t));
#endif
  *(--global->data_stack) =
    value < 32768 ? value : value | (((hf_sign_cell_t)-1) & ~0xFFFF);
}

/* (LITW) primitive */
void hf_prim_lit_32(hf_global_t* global) {
  hf_cell_t value;
#ifndef TOKEN_32
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(uint32_t));
#endif
  value = *(uint32_t*)global->ip;
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(uint32_t));
  *(--global->data_stack) =
    value < 2147483648 ? value : value | (((hf_sign_cell_t)-1) & ~0xFFFFFFFF);
}

/* (DATA) primitive */
void hf_prim_data(hf_global_t* global) {
  hf_cell_t bytes;
#ifdef CELL_16
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#elif CELL_32
#ifdef TOKEN_8_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#elif TOKEN_16
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
#else
  global->ip = (hf_token_t*)HF_ALIGNED_TO(global->ip, sizeof(hf_cell_t));
#endif
  bytes = *(hf_cell_t*)global->ip;
  *(--global->data_stack) = (hf_cell_t)global->ip + sizeof(hf_cell_t);
  global->ip =
    (hf_token_t*)HF_ALIGNED_TO_TOKEN((void*)global->ip +
				     sizeof(hf_cell_t) + bytes);
}

/* NEW-COLON primitive */
void hf_prim_new_colon(hf_global_t* global) {
  hf_word_t* word;
  hf_full_token_t token = hf_new_token(global);
  word = hf_new_word(global, token);
  word->data = NULL;
  word->primitive = hf_prim_enter;
  word->secondary = HF_ALIGNED_TO_TOKEN(*global->data_stack++);
  *(--global->data_stack) = (hf_cell_t)token;
}

/* NEW-CREATE primitive */
void hf_prim_new_create(hf_global_t* global) {
  hf_word_t* word;
  hf_full_token_t token = hf_new_token(global);
  word = hf_new_word(global, token);
  word->data = (void*)(*global->data_stack++);
  word->primitive = hf_prim_do_create;
  word->secondary = NULL;
  *(--global->data_stack) = (hf_cell_t)token;
}

/* SET-DOES> primitive */
void hf_prim_set_does(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    word->primitive = hf_prim_do_does;
    word->secondary = HF_ALIGNED_TO_TOKEN(*global->return_stack++);
    global->ip = *global->return_stack++;
  } else {
    fprintf(stderr, "Out of range token!\n");
    exit(1);
  }
}

/* FINISH primitive */
void hf_prim_finish(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  if(token < global->word_count) {
    // In a practical implementation, something will be done here.
  } else {
    fprintf(stderr, "Out of range token!\n");
  }
}

/* EXECUTE primitive */
void hf_prim_execute(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    global->current_word = word;
#ifdef TRACE
    if(global->trace) {
      hf_sign_cell_t level = global->return_stack_base - global->return_stack;
      /* fprintf(stderr, "[%lld] ", level); */
      for(hf_sign_cell_t i = 0; i < level; i++) {
      	fprintf(stderr, "  ");
      }
      if(global->name_table && global->name_table[token].name_length) {
	char* name_copy = malloc(global->name_table[token].name_length);
	memcpy(name_copy, global->name_table[token].name,
	       global->name_table[token].name_length);
	name_copy[global->name_table[token].name_length] = 0;
	fprintf(stderr, "executing token: %lld name: %s data stack: %lld",
	       (uint64_t)token, name_copy, (uint64_t)global->data_stack);
	free(name_copy);
      } else {
	fprintf(stderr, "executing token: %lld <no name> data stack: %lld",
	       (uint64_t)token, (uint64_t)global->data_stack);
      }
#ifdef STACK_TRACE
      fprintf(stderr, " [");
      hf_cell_t* stack_trace = global->data_stack;
      hf_cell_t count = 10;
      while(stack_trace < global->data_stack_base && count--) {
	fprintf(stderr, " %lld", (uint64_t)(*stack_trace++));
      }
      if(stack_trace < global->data_stack_base && !count) {
	fprintf(stderr, " ...");
      }
      fprintf(stderr, " ]\n");
#else
      fprintf(stderr, "\n");
#endif
    }
#endif
    word->primitive(global);
  } else {
    hf_set_int(global, HF_INT_TOKEN, HF_TRUE);
  }
}

/* DROP primitive */
void hf_prim_drop(hf_global_t* global) {
  global->data_stack++;
}

/* DUP primitive */
void hf_prim_dup(hf_global_t* global) {
  hf_cell_t value = *global->data_stack;
  *(--global->data_stack) = value;
}

/* SWAP primitive */
void hf_prim_swap(hf_global_t* global) {
  hf_cell_t value = *global->data_stack;
  *global->data_stack = *(global->data_stack + 1);
  *(global->data_stack + 1) = value;
}

/* OVER primitive */
void hf_prim_over(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1);
  *(--global->data_stack) = value;
}

/* ROT primitive */
void hf_prim_rot(hf_global_t* global) {
  hf_cell_t value0 = *global->data_stack;
  hf_cell_t value1 = *(global->data_stack + 1);
  hf_cell_t value2 = *(global->data_stack + 2);
  *global->data_stack = value2;
  *(global->data_stack + 1) = value0;
  *(global->data_stack + 2) = value1;
}

/* PICK primitive */
void hf_prim_pick(hf_global_t* global) {
  *global->data_stack = *(global->data_stack + *global->data_stack + 1);
}

/* ROLL primitive */
void hf_prim_roll(hf_global_t* global) {
  hf_cell_t count = *global->data_stack++;
  hf_cell_t end_value = *(global->data_stack + count);
  while(count) {
    *(global->data_stack + count) = *(global->data_stack + count - 1);
    count--;
  }
  *global->data_stack = end_value;
}

/* @ primitive */
void hf_prim_load(hf_global_t* global) {
  *global->data_stack = *(hf_cell_t*)(*global->data_stack);
}

/* ! primitive */
void hf_prim_store(hf_global_t* global) {
  *(hf_cell_t*)(*global->data_stack) = *(global->data_stack + 1);
  global->data_stack += 2;
}

/* C@ primitive */
void hf_prim_load_8(hf_global_t* global) {
  *global->data_stack = *(hf_byte_t*)(*global->data_stack);
}

/* C! primitive */
void hf_prim_store_8(hf_global_t* global) {
  *(hf_byte_t*)(*global->data_stack) = *(global->data_stack + 1) & 0xFF;
  global->data_stack += 2;
}

/* = primitive */
void hf_prim_eq(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) == *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* <> primitive */
void hf_prim_ne(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) != *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* < primitive */
void hf_prim_lt(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(hf_sign_cell_t*)(global->data_stack + 1) <
	    *(hf_sign_cell_t*)global->data_stack);
  *(++global->data_stack) = comparison;
}

/* > primitive */
void hf_prim_gt(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(hf_sign_cell_t*)(global->data_stack + 1) >
	    *(hf_sign_cell_t*)global->data_stack);
  *(++global->data_stack) = comparison;
}

/* U< primitive */
void hf_prim_ult(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) < *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* U> primitive */
void hf_prim_ugt(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) > *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* NOT primitive */
void hf_prim_not(hf_global_t* global) {
  *global->data_stack = ~(*global->data_stack);
}

/* AND primitive */
void hf_prim_and(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) & *global->data_stack;
  *(++global->data_stack) = value;
}

/* OR primitive */
void hf_prim_or(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) | *global->data_stack;
  *(++global->data_stack) = value;
}

/* XOR primitive */
void hf_prim_xor(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) ^ *global->data_stack;
  *(++global->data_stack) = value;
}

/* LSHIFT primitive */
void hf_prim_lshift(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) << *global->data_stack;
  *(++global->data_stack) = value;
}

/* RSHIFT primitive */
void hf_prim_rshift(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) >> *global->data_stack;
  *(++global->data_stack) = value;
}

/* ARSHIFT primitive */
void hf_prim_arshift(hf_global_t* global) {
  hf_sign_cell_t value = *(hf_sign_cell_t*)(global->data_stack + 1) >>
    *global->data_stack;
  *(hf_sign_cell_t*)(++global->data_stack) = value;
}

/* + primitive */
void hf_prim_add(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) + *global->data_stack;
  *(++global->data_stack) = value;
}

/* - primitive */
void hf_prim_sub(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) - *global->data_stack;
  *(++global->data_stack) = value;
}

/* * primitive */
void hf_prim_mul(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) * *global->data_stack;
  *(++global->data_stack) = value;
}

/* / primitive */
void hf_prim_div(hf_global_t* global) {
  hf_sign_cell_t value = *(hf_sign_cell_t*)(global->data_stack + 1) /
    *(hf_sign_cell_t*)global->data_stack;
  *(hf_sign_cell_t*)(++global->data_stack) = value;
}

/* MOD primitive */
void hf_prim_mod(hf_global_t* global) {
  hf_sign_cell_t value = *(hf_sign_cell_t*)(global->data_stack + 1) %
    *(hf_sign_cell_t*)global->data_stack;
  *(hf_sign_cell_t*)(++global->data_stack) = value;
}

/* U/ primitive */
void hf_prim_udiv(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) / *global->data_stack;
  *(++global->data_stack) = value;
}

/* UMOD primitive */
void hf_prim_umod(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) % *global->data_stack;
  *(++global->data_stack) = value;
}

/* MUX primitive */
void hf_prim_mux(hf_global_t* global) {
  hf_cell_t value0 = *(global->data_stack + 2);
  hf_cell_t value1 = *(global->data_stack + 1);
  hf_cell_t mux_value = *global->data_stack;
  global->data_stack += 2;
  *global->data_stack = (value0 & mux_value) | (value1 & ~mux_value);
}

/* /MUX primitive */
void hf_prim_rmux(hf_global_t* global) {
  hf_cell_t mux_value = *(global->data_stack + 2);
  hf_cell_t value0 = *(global->data_stack + 1);
  hf_cell_t value1 = *global->data_stack;
  global->data_stack += 2;
  *global->data_stack = (value0 & mux_value) | (value1 & ~mux_value);
}

/* R@ primitive */
void hf_prim_load_r(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)(*global->return_stack);
}

/* >R primitive */
void hf_prim_push_r(hf_global_t* global) {
  *(--global->return_stack) = (hf_token_t*)(*global->data_stack++);
}

/* R> primitive */
void hf_prim_pop_r(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)(*global->return_stack++);
}

/* SP@ primitive */
void hf_prim_load_sp(hf_global_t* global) {
  hf_cell_t* data_stack = global->data_stack;
  *(--global->data_stack) = (hf_cell_t)data_stack;
}

/* SP! primitive */
void hf_prim_store_sp(hf_global_t* global) {
  global->data_stack = (hf_cell_t*)(*global->data_stack);
  global->old_data_stack_base = global->data_stack_base;
}

/* RP@ primitive */
void hf_prim_load_rp(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->return_stack;
}

/* RP! primitive */
void hf_prim_store_rp(hf_global_t* global) {
  global->return_stack = (hf_token_t**)(*global->data_stack++);
}

/* >BODY primitive */
void hf_prim_to_body(hf_global_t* global) {
  hf_cell_t token = *global->data_stack;
  if(token < global->word_count) {
    *global->data_stack = (hf_cell_t)global->words[token].data;
  } else {
    fprintf(stderr, "Out of range token!\n");
    exit(1);
  }
}

/* H@ primitive */
void hf_prim_load_16(hf_global_t* global) {
  *global->data_stack = *(uint16_t*)(*global->data_stack);
}

/* H! primitive */
void hf_prim_store_16(hf_global_t* global) {
  *(uint16_t*)(*global->data_stack) = *(global->data_stack + 1) & 0xFFFF;
  global->data_stack += 2;
}

/* W@ primitive */
void hf_prim_load_32(hf_global_t* global) {
#ifndef CELL_16
  *global->data_stack = *(uint32_t*)(*global->data_stack);
#else
  fprintf(stderr, "32-bit values not supported!\n");
#endif
}

/* W! primitive */
void hf_prim_store_32(hf_global_t* global) {
#ifndef CELL_16
  *(uint32_t*)(*global->data_stack) = *(global->data_stack + 1) & 0xFFFFFFFF;
  global->data_stack += 2;
#else
  fprintf(stderr, "32-bit values not supported!\n");
#endif
}

/* SET-WORD-COUNT primitive */
void hf_prim_set_word_count(hf_global_t* global) {
  hf_cell_t new_word_count = *global->data_stack++;
  if(new_word_count > 0 && new_word_count <= global->word_count) {
    global->word_count = new_word_count;
  } else {
    fprintf(stderr, "Attempted to set out of range word count!\n");
    exit(1);
  }
}

/* SYS primitive */
void hf_prim_sys(hf_global_t* global) {
  hf_sys_index_t index = (hf_sys_index_t)(*global->data_stack++);
  if(index >= 0) {
    if(index < global->std_service_count) {
      if(global->std_services[index].defined) {
	global->std_services[index].primitive(global);
	*(--global->data_stack) = HF_TRUE;
      } else {
	*(--global->data_stack) = HF_FALSE;
      }
    }
  } else {
    index = -index - 1;
    if(index < global->nstd_service_count) {
      if(global->nstd_services[index].defined) {
	global->nstd_services[index].primitive(global);
	*(--global->data_stack) = HF_TRUE;
      } else {
	*(--global->data_stack) = HF_FALSE;
      }
    }
  }
}

/* D+ primitive */
void hf_prim_d_add(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 + value1);
}

/* D- primitive */
void hf_prim_d_sub(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 - value1);
}

/* D* primitive */
void hf_prim_d_mul(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 * value1);
}

/* D/ primitive */
void hf_prim_d_div(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 / value1);
}

/* DMOD primitive */
void hf_prim_d_mod(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 % value1);
}

/* DU/ primitive */
void hf_prim_d_udiv(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 / value1);
}

/* DUMOD primitive */
void hf_prim_d_umod(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 % value1);
}

/* DNOT primitive */
void hf_prim_d_not(hf_global_t* global) {
  hf_2cell_t value;
  HF_LOAD_2CELL(global, 0, value);
  HF_STORE_2CELL(global, 0, ~value);
}

/* DAND primitive */
void hf_prim_d_and(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 & value1);
}

/* DOR primitive */
void hf_prim_d_or(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 | value1);
}

/* DXOR primitive */
void hf_prim_d_xor(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 2;
  HF_STORE_2CELL(global, 0, value0 ^ value1);
}

/* DLSHIFT primitive */
void hf_prim_d_lshift(hf_global_t* global) {
  hf_2cell_t value0;
  hf_cell_t value1;
  HF_LOAD_2CELL(global, 1, value0);
  value1 = *global->data_stack++;
  HF_STORE_2CELL(global, 0, value0 << value1);
}

/* DRSHIFT primitive */
void hf_prim_d_rshift(hf_global_t* global) {
  hf_2cell_t value0;
  hf_cell_t value1;
  HF_LOAD_2CELL(global, 1, value0);
  value1 = *global->data_stack++;
  HF_STORE_2CELL(global, 0, value0 >> value1);
}

/* DARSHIFT primitive */
void hf_prim_d_arshift(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 1, value0);
  value1 = *global->data_stack++;
  HF_STORE_2CELL(global, 0, value0 >> value1);
}

/* D< primitive */
void hf_prim_d_lt(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 3;
  *global->data_stack = (hf_cell_t)(value0 < value1 ? -1 : 0);
}

/* D> primitive */
void hf_prim_d_gt(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 3;
  *global->data_stack = (hf_cell_t)(value0 > value1 ? -1 : 0);
}

/* D= primitive */
void hf_prim_d_eq(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 3;
  *global->data_stack = (hf_cell_t)(value0 == value1 ? -1 : 0);
}

/* D<> primitive */
void hf_prim_d_ne(hf_global_t* global) {
  hf_sign_2cell_t value0;
  hf_sign_2cell_t value1;
  HF_LOAD_SIGN_2CELL(global, 2, value0);
  HF_LOAD_SIGN_2CELL(global, 0, value1);
  global->data_stack += 3;
  *global->data_stack = (hf_cell_t)(value0 != value1 ? -1 : 0);
}

/* DU< primitive */
void hf_prim_d_ult(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 3;
  *global->data_stack = (hf_cell_t)(value0 < value1 ? -1 : 0);
}

/* DU> primitive */
void hf_prim_d_ugt(hf_global_t* global) {
  hf_2cell_t value0;
  hf_2cell_t value1;
  HF_LOAD_2CELL(global, 2, value0);
  HF_LOAD_2CELL(global, 0, value1);
  global->data_stack += 3;
  *global->data_stack = (hf_cell_t)(value0 > value1 ? -1 : 0);
}

/* * / primitive */
void hf_prim_mul_div(hf_global_t* global) {
  hf_sign_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_sign_2cell_t value1 = *(hf_sign_cell_t*)(global->data_stack + 1);
  hf_sign_2cell_t value2 = *(hf_sign_cell_t*)(global->data_stack);
  hf_sign_2cell_t result = (value0 * value1) / value2;
  global->data_stack += 2;
  *global->data_stack = (hf_sign_cell_t)result;
}

/* *RSHIFT primitive */
void hf_prim_mul_rshift(hf_global_t* global) {
  hf_sign_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_sign_2cell_t value1 = *(hf_sign_cell_t*)(global->data_stack + 1);
  hf_cell_t value2 = *global->data_stack;
  hf_sign_2cell_t result = (value0 * value1) >> value2;
  global->data_stack += 2;
  *global->data_stack = (hf_sign_cell_t)result;
}

/* LSHIFT/ primitive */
void hf_prim_lshift_div(hf_global_t* global) {
  hf_sign_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_cell_t value1 = *(global->data_stack + 1);
  hf_sign_2cell_t value2 = *global->data_stack;
  hf_sign_2cell_t result = (value0 << value1) / value2;
  global->data_stack += 2;
  *global->data_stack = (hf_sign_cell_t)result;
}

/* U* / primitive */
void hf_prim_umul_div(hf_global_t* global) {
  hf_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_2cell_t value1 = *(hf_sign_cell_t*)(global->data_stack + 1);
  hf_2cell_t value2 = *(hf_sign_cell_t*)(global->data_stack);
  hf_2cell_t result = (value0 * value1) / value2;
  global->data_stack += 2;
  *global->data_stack = result;
}

/* U*RSHIFT primitive */
void hf_prim_umul_rshift(hf_global_t* global) {
  hf_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_2cell_t value1 = *(hf_sign_cell_t*)(global->data_stack + 1);
  hf_cell_t value2 = *global->data_stack;
  hf_2cell_t result = (value0 * value1) >> value2;
  global->data_stack += 2;
  *global->data_stack = result;
}

/* ULSHIFT/ primitive */
void hf_prim_ulshift_div(hf_global_t* global) {
  hf_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_cell_t value1 = *(global->data_stack + 1);
  hf_2cell_t value2 = *global->data_stack;
  hf_2cell_t result = (value0 << value1) / value2;
  global->data_stack += 2;
  *global->data_stack = result;
}

/* * /MOD primitive */
void hf_prim_mul_div_mod(hf_global_t* global) {
  hf_sign_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_sign_2cell_t value1 = *(hf_sign_cell_t*)(global->data_stack + 1);
  hf_sign_2cell_t value2 = *(hf_sign_cell_t*)(global->data_stack);
  hf_sign_2cell_t product = value0 * value1;
  hf_sign_2cell_t remainder = product % value2;
  hf_sign_2cell_t quotient = product / value2;
  global->data_stack++;
  *(global->data_stack + 1) = (hf_sign_cell_t)remainder;
  *global->data_stack = (hf_sign_cell_t)quotient;
}

/* U* /MOD primitive */
void hf_prim_umul_div_mod(hf_global_t* global) {
  hf_2cell_t value0 = *(hf_sign_cell_t*)(global->data_stack + 2);
  hf_2cell_t value1 = *(hf_sign_cell_t*)(global->data_stack + 1);
  hf_2cell_t value2 = *(hf_sign_cell_t*)(global->data_stack);
  hf_2cell_t product = value0 * value1;
  hf_2cell_t remainder = product % value2;
  hf_2cell_t quotient = product / value2;
  global->data_stack++;
  *(global->data_stack + 1) = remainder;
  *global->data_stack = quotient;
}
