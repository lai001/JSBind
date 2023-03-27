import { A, B, SharedPtrB, BB, BBB } from "ks";

for (let index = 0; index < 2; index++) {
    let a = new A(50 + index, 150 + index);
    let b = new B();
    a.b = b;
    b.data = 1500 + index;
    console.log(a.b.data, b.data);
    console.log(a.b == b);
    console.log(a.v4, a.v6);
    a.printB();
    console.log(b.str);
}

console.log(1, g_b);
console.log(2, g_b.str);
console.log(3, g_b1);
console.log(4, g_b1.str);

let shared_ptr_b = new SharedPtrB();

console.log(shared_ptr_b);
console.log(5, shared_ptr_b.str);
console.log(6, g_sharedptr_b);
console.log(7, g_sharedptr_b.str);
console.log(8, g_sharedptr_b1);
console.log(9, g_sharedptr_b1.str);

try {
    let a = new A();
    a.b = shared_ptr_b;
} catch (error) {
    console.log(10, error);
}

try {
    let a = new A();
    a.b = shared_ptr_b._get();
} catch (error) {
    console.log(11, error);
}

let bb = new BB();
console.log(12, `data0: ${bb.data0}, data1: ${bb.data1} str: ${bb.str}`);

let bbb = new BBB();
console.log(13, `data0: ${bb.data0}, data1: ${bb.data1}, data2: ${bbb.data2}, str: ${bbb.str}`);

let b = new B();
b.cppvector = [1, 2, 3];
b.cppvector.set(2, 200)
console.log(b.cppvector.get(2));


globalThis.array = [1, 2, 3, 4, 4]