use criterion::{black_box, criterion_group, criterion_main, Criterion};
use rand::thread_rng;
use rust_zca_bench::*;

fn fibonacci(n: u64) -> u64 {
    match n {
        0 => 1,
        1 => 1,
        n => fibonacci(n - 1) + fibonacci(n - 2),
    }
}

fn criterion_benchmark(c: &mut Criterion) {
    c.bench_function("fib 20", |b| b.iter(|| fibonacci(black_box(20))));
}

fn function_benchmark(c: &mut Criterion) {
    // around 8MB dataset
    const VEC_LENGTH: usize = 20_000;
    const NUM_VECS: usize = 100;

    let mut rng = thread_rng();
    let test_set = TestSet::create(VEC_LENGTH, NUM_VECS, &mut rng);

    c.bench_function("rng select baseline", |b| {
        b.iter(|| test_set.sample_pair(&mut rng))
    });

    c.bench_function("calculate_direct", |b| {
        b.iter(|| {
            let vecs = test_set.sample_pair(&mut rng);
            let res = calculate_direct(vecs.0, vecs.1);
            res
        })
    });

    c.bench_function("calculate_iter", |b| {
        b.iter(|| {
            let vecs = test_set.sample_pair(&mut rng);
            let res = calculate_iter(vecs.0, vecs.1);
            res
        })
    });

    c.bench_function("calculate_fold", |b| {
        b.iter(|| {
            let vecs = test_set.sample_pair(&mut rng);
            let res = calculate_fold(vecs.0, vecs.1);
            res
        })
    });
}

criterion_group!(benches, function_benchmark, criterion_benchmark);
criterion_main!(benches);
