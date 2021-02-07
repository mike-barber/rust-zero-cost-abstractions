use std::iter;

use rand::{distributions::Uniform, Rng};

pub struct TestSet {
    vectors: Vec<Vec<i32>>,
}
impl TestSet {
    pub fn create<R>(vec_length: usize, num_vecs: usize, rng: &mut R) -> TestSet
    where
        R: Rng,
    {
        let uniform = Uniform::new(0, 10);
        let mut vecs = Vec::new();
        for _ in 0..num_vecs {
            let v = rng.sample_iter(uniform).take(vec_length).collect();
            vecs.push(v);
        }
        TestSet { vectors: vecs }
    }

    pub fn sample_pair<R>(&self, rng: &mut R) -> (&[i32], &[i32])
    where
        R: Rng,
    {
        use rand::seq::SliceRandom;

        let vv1 = self.vectors.choose(rng).unwrap();
        let vv2 = self.vectors.choose(rng).unwrap();
        (vv1, vv2)
    }
}

pub fn calculate_direct(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    let mut res = 0;
    for (a, b) in slice_a.iter().zip(slice_b.iter()) {
        if *a > 2 {
            res += *a as i64 * *b as i64;
        }
    }
    res
}

pub fn calculate_iter(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    slice_a
        .iter()
        .zip(slice_b.iter())
        .filter(|(&a, &_b)| a > 2)
        .map(|(&a, &b)| a as i64 * b as i64)
        .sum()
}

pub fn calculate_fold(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    slice_a
        .iter()
        .zip(slice_b.iter())
        .fold(0_i64, |acc, (a, b)| match *a > 2 {
            true => acc + (*a as i64 * *b as i64),
            false => acc,
        })
}

#[cfg(test)]
mod tests {
    #[test]
    fn it_works() {
        assert_eq!(2 + 2, 4);
    }
}
