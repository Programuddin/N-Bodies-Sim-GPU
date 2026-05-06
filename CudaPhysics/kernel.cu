#include <cuda_runtime.h>
#include <device_launch_parameters.h>
#include <math.h>
#include <string.h> 

#define G_CONST 6.67430e-11
#define MAX_BODIES 1024 

static double* d_posX = nullptr, * d_posY = nullptr;
static double* d_velX = nullptr, * d_velY = nullptr;
static double* d_mass = nullptr;
static int currentNumBodies = 0;
static cudaStream_t physicsStream = nullptr;

static double* h_posX = nullptr, * h_posY = nullptr;
static double* h_velX = nullptr, * h_velY = nullptr;
static double* h_mass = nullptr;

static void AllocDevice(double** ptr, int n) {
    cudaFree(*ptr); cudaMalloc(ptr, n * sizeof(double));
}
static void AllocPinned(double** ptr, int n) {
    cudaFreeHost(*ptr); cudaMallocHost(ptr, n * sizeof(double));
}

__global__ void CalculateRK4_MegaKernel(double* pX, double* pY, double* vX, double* vY, double* mass, int numBodies, double dt, int subSteps) {

    __shared__ double s_pX[MAX_BODIES];
    __shared__ double s_pY[MAX_BODIES];
    __shared__ double s_mass[MAX_BODIES];
    __shared__ double s_hypoX[MAX_BODIES];
    __shared__ double s_hypoY[MAX_BODIES];

    int i = threadIdx.x;
    double my_pX = 0, my_pY = 0, my_vX = 0, my_vY = 0;

    if (i < numBodies) {
        s_pX[i] = pX[i];
        s_pY[i] = pY[i];
        s_mass[i] = mass[i];
        my_pX = pX[i];
        my_pY = pY[i];
        my_vX = vX[i];
        my_vY = vY[i];
    }
    __syncthreads();

    double stepDt = dt / subSteps;
    double halfDt = stepDt * 0.5;

    for (int step = 0; step < subSteps; step++) {

        double k1_vX = my_vX;
        double k1_vY = my_vY;

        double k1_aX = 0, k1_aY = 0;
        if (i < numBodies) {
            for (int j = 0; j < numBodies; j++) {
                if (i == j) continue;
                double dx = s_pX[j] - my_pX;
                double dy = s_pY[j] - my_pY;
                double distSq = dx * dx + dy * dy + 1e-10;

                double inv = rsqrt(distSq);
                double f = G_CONST * s_mass[j] * inv * inv * inv;

                k1_aX += f * dx; k1_aY += f * dy;
            }
            s_hypoX[i] = my_pX + k1_vX * halfDt;
            s_hypoY[i] = my_pY + k1_vY * halfDt;
        }
        __syncthreads();

        double k2_aX = 0, k2_aY = 0;
        if (i < numBodies) {
            for (int j = 0; j < numBodies; j++) {
                if (i == j) continue;
                double dx = s_hypoX[j] - s_hypoX[i];
                double dy = s_hypoY[j] - s_hypoY[i];
                double distSq = dx * dx + dy * dy + 1e-10;
                double inv = rsqrt(distSq);
                double f = G_CONST * s_mass[j] * inv * inv * inv;
                k2_aX += f * dx; k2_aY += f * dy;
            }
        }
        __syncthreads();

        double k2_vX = my_vX + k1_aX * halfDt;
        double k2_vY = my_vY + k1_aY * halfDt;
        if (i < numBodies) {
            s_hypoX[i] = my_pX + k2_vX * halfDt;
            s_hypoY[i] = my_pY + k2_vY * halfDt;
        }
        __syncthreads();

        double k3_aX = 0, k3_aY = 0;
        if (i < numBodies) {
            for (int j = 0; j < numBodies; j++) {
                if (i == j) continue;
                double dx = s_hypoX[j] - s_hypoX[i];
                double dy = s_hypoY[j] - s_hypoY[i];
                double distSq = dx * dx + dy * dy + 1e-10;
                double inv = rsqrt(distSq);
                double f = G_CONST * s_mass[j] * inv * inv * inv;
                k3_aX += f * dx; k3_aY += f * dy;
            }
        }
        __syncthreads();

        double k3_vX = my_vX + k2_aX * halfDt;
        double k3_vY = my_vY + k2_aY * halfDt;
        if (i < numBodies) {
            s_hypoX[i] = my_pX + k3_vX * stepDt;
            s_hypoY[i] = my_pY + k3_vY * stepDt;
        }
        __syncthreads();

        double k4_aX = 0, k4_aY = 0;
        if (i < numBodies) {
            for (int j = 0; j < numBodies; j++) {
                if (i == j) continue;
                double dx = s_hypoX[j] - s_hypoX[i];
                double dy = s_hypoY[j] - s_hypoY[i];
                double distSq = dx * dx + dy * dy + 1e-10;
                double inv = rsqrt(distSq);
                double f = G_CONST * s_mass[j] * inv * inv * inv;
                k4_aX += f * dx; k4_aY += f * dy;
            }

            double k4_vX = my_vX + k3_aX * stepDt;
            double k4_vY = my_vY + k3_aY * stepDt;

            my_vX += (k1_aX + 2.0 * k2_aX + 2.0 * k3_aX + k4_aX) * stepDt / 6.0;
            my_vY += (k1_aY + 2.0 * k2_aY + 2.0 * k3_aY + k4_aY) * stepDt / 6.0;
            my_pX += (k1_vX + 2.0 * k2_vX + 2.0 * k3_vX + k4_vX) * stepDt / 6.0;
            my_pY += (k1_vY + 2.0 * k2_vY + 2.0 * k3_vY + k4_vY) * stepDt / 6.0;

            s_pX[i] = my_pX;
            s_pY[i] = my_pY;
        }
        __syncthreads();
    }

    if (i < numBodies) {
        pX[i] = my_pX; pY[i] = my_pY;
        vX[i] = my_vX; vY[i] = my_vY;
    }
}

extern "C" {
    __declspec(dllexport) void InitCudaMemory(int numBodies) {
        if (currentNumBodies == numBodies) return;

        cudaSetDeviceFlags(cudaDeviceScheduleSpin);

        if (physicsStream) cudaStreamDestroy(physicsStream);
        cudaStreamCreate(&physicsStream);

        AllocDevice(&d_posX, numBodies); AllocDevice(&d_posY, numBodies);
        AllocDevice(&d_velX, numBodies); AllocDevice(&d_velY, numBodies);
        AllocDevice(&d_mass, numBodies);

        AllocPinned(&h_posX, numBodies); AllocPinned(&h_posY, numBodies);
        AllocPinned(&h_velX, numBodies); AllocPinned(&h_velY, numBodies);
        AllocPinned(&h_mass, numBodies);

        currentNumBodies = numBodies;
    }

    __declspec(dllexport) void UpdatePhysicsCUDA(double* posX, double* posY, double* velX, double* velY, double* mass, int numBodies, double dt, int subSteps) {
        size_t bytes = numBodies * sizeof(double);

        memcpy(h_posX, posX, bytes); memcpy(h_posY, posY, bytes);
        memcpy(h_velX, velX, bytes); memcpy(h_velY, velY, bytes);
        memcpy(h_mass, mass, bytes);

        cudaMemcpyAsync(d_posX, h_posX, bytes, cudaMemcpyHostToDevice, physicsStream);
        cudaMemcpyAsync(d_posY, h_posY, bytes, cudaMemcpyHostToDevice, physicsStream);
        cudaMemcpyAsync(d_velX, h_velX, bytes, cudaMemcpyHostToDevice, physicsStream);
        cudaMemcpyAsync(d_velY, h_velY, bytes, cudaMemcpyHostToDevice, physicsStream);
        cudaMemcpyAsync(d_mass, h_mass, bytes, cudaMemcpyHostToDevice, physicsStream);

        int threads = numBodies;
        if (threads > 1024) threads = 1024; 
        int blocks = 1;

        CalculateRK4_MegaKernel << <blocks, threads, 0, physicsStream >> > (d_posX, d_posY, d_velX, d_velY, d_mass, numBodies, dt, subSteps);

        cudaMemcpyAsync(h_posX, d_posX, bytes, cudaMemcpyDeviceToHost, physicsStream);
        cudaMemcpyAsync(h_posY, d_posY, bytes, cudaMemcpyDeviceToHost, physicsStream);
        cudaMemcpyAsync(h_velX, d_velX, bytes, cudaMemcpyDeviceToHost, physicsStream);
        cudaMemcpyAsync(h_velY, d_velY, bytes, cudaMemcpyDeviceToHost, physicsStream);

        cudaStreamSynchronize(physicsStream);

        memcpy(posX, h_posX, bytes); memcpy(posY, h_posY, bytes);
        memcpy(velX, h_velX, bytes); memcpy(velY, h_velY, bytes);
    }
}