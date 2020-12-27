from nn import NNTraining

training = NNTraining(inputFile='sino-ml-purify.json', outputFile='sino-ml-purify.txt')
training.run(numFeatures=16*16, numClasses=5, numHidden1=64, numSteps=10000)
