// import { A, B, SharedPtrB, BB, BBB } from "ks";

// for (let index = 0; index < 2; index++) {
//     let a = new A(50 + index, 150 + index);
//     let b = new B();
//     a.b = b;
//     b.data = 1500 + index;
//     console.log(a.b.data, b.data);
//     console.log(a.b == b);
//     console.log(a.v4, a.v6);
//     a.printB();
//     console.log(b.str);
// }

// console.log(1, g_b);
// console.log(2, g_b.str);
// console.log(3, g_b1);
// console.log(4, g_b1.str);

// let shared_ptr_b = new SharedPtrB();

// console.log(5, shared_ptr_b);
// console.log(6, shared_ptr_b.str);
// console.log(7, g_sharedptr_b);
// console.log(8, g_sharedptr_b.str);
// console.log(9, g_sharedptr_b1);
// console.log(10, g_sharedptr_b1.str);

// try {
//     let a = new A();
//     a.b = shared_ptr_b;
// } catch (error) {
//     console.log(11, error);
// }

// try {
//     let a = new A();
//     a.b = shared_ptr_b._get();
// } catch (error) {
//     console.log(12, error);
// }

// let bb = new BB();
// console.log(13, `data0: ${bb.data0}, data1: ${bb.data1} str: ${bb.str}`);

// let bbb = new BBB();
// console.log(14, `data0: ${bb.data0}, data1: ${bb.data1}, data2: ${bbb.data2}, str: ${bbb.str}`);

// let b = new B();
// b.cppvector = [1, 2, 3];
// b.cppvector.set(2, 200)
// console.log(15, b.cppvector.get(2));


// globalThis.array = [1, 2, 3, 4, 4]

// // function fib(n) {
// //     if (n <= 0) {
// //         return 0;
// //     }
// //     if (n < 3)
// //         return 1;
// //     return fib(n - 1) + fib(n - 2);
// // }
// // let time = new Date().getTime();
// // for (let i = 0; i < 45; i++) {
// //     fib(i);
// // }
// // let time1 = new Date().getTime();
// // console.log(16, time1 - time);


let bb = new BB();
let b = new B(bb);
console.log(b.data0);
console.log(b.data1);
console.log(bb.data1);
// a.v4 = 1000;
// console.log(a.v4);